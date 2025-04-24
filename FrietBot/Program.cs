using FrietBot;
using FrietBot.Handlers;
using FrietBot.Jobs;
using FrietBot.Models;
using FrietBot.Services;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using SlackNet.AspNetCore;
using SlackNet.Blocks;
using SlackNet.Events;
using StackExchange.Redis;

// Configure logger first
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebsite", policy =>
    {
        policy.WithOrigins(
                OrderConfig.OrderingLink,
                "http://localhost:3000",
                "http://localhost:5173"
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var slackSettings = builder.Configuration.GetSection("Slack").Get<SlackSettings>()!;

// Configure Redis
var redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>() ?? new RedisConfig
{
    ConnectionString = "localhost:6379",
    Database = 0
};

// Create Redis configuration with in-memory database
var redisConfiguration = ConfigurationOptions.Parse(redisConfig.ConnectionString);
redisConfiguration.AllowAdmin = true; // Enable admin commands
redisConfiguration.SyncTimeout = 5000; // 5 seconds timeout

var redis = ConnectionMultiplexer.Connect(redisConfiguration);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddSingleton<IRedisService, RedisService>();
builder.Services.AddSingleton<IMenuService, MenuService>();

Log.Information("Configuring SlackNet...");

builder.Services.AddSlackNet(c => c
    // Configure the tokens used to authenticate with Slack
    .UseApiToken(slackSettings.ApiToken) // This gets used by the API client
    .UseAppLevelToken(slackSettings.AppLevelToken) // (Optional) used for socket mode
    
    // The signing secret ensures that SlackNet only handles requests from Slack 
    .UseSigningSecret(slackSettings.SigningSecret)
    
    // Register your Slack handlers here
    .RegisterEventHandler<MessageEvent, FrietNotification>()
    .RegisterEventHandler<MessageEvent, QuantityHandler>()
    .RegisterBlockActionHandler<ButtonAction, FrietDialog>()
    .RegisterViewSubmissionHandler<FrietDialog>("friet_order")
    .RegisterSlashCommandHandler<TotalOrderCommand>("/bestelling")
    .RegisterEventHandler<AppHomeOpened, HomeTabHandler>()
);

Log.Information("SlackNet configured with event handlers:");
Log.Information("- MessageEvent -> FrietNotification");
Log.Information("- MessageEvent -> QuantityHandler");
Log.Information("- ButtonAction -> FrietDialog");
Log.Information("- ViewSubmission -> FrietDialog (friet_order)");
Log.Information("- SlashCommand -> TotalOrderCommand (/bestelling)");
Log.Information("- AppHomeOpened -> HomeTabHandler");

builder.Services.AddSerilog();

builder.Services.AddSingleton<ISlackService, SlackService>();
builder.Services.AddSingleton<OrderJob>();
builder.Services.AddSingleton<CloseOrdersJob>();
builder.Services.AddSingleton<ResetCycleJob>();
builder.Services.AddSingleton<ClearOrdersJob>();
builder.Services.AddSingleton<IOrderStatusService, OrderStatusService>();
    
// Register Quartz services
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddSingleton<IJobFactory, JobFactory>();
builder.Services.AddSingleton<SchedulerService>();
    
// Register hosted service to start/stop the scheduler with the application
builder.Services.AddHostedService<SchedulerHostedService>();

var app = builder.Build();

// Enable CORS
app.UseCors("AllowWebsite");

// This sets up the SlackNet endpoints for handling requests from Slack
// By default the endpoints are /slack/event, /slack/action, /slack/options, and /slack/command,
// but the 'slack' prefix can be changed using MapToPrefix.
app.UseSlackNet(c => c
    // You can enable socket mode for testing without having to make your web app publicly accessible
    .UseSocketMode(false)
);

app.MapGet("/ping", () => "Pong!");

app.MapGet("/api/menu", (IMenuService menuService) =>
{
    var menuConfig = menuService.GetMenuConfig();
    return Results.Ok(menuConfig);
});

app.MapGet("/api/orders", async (IRedisService redisService) =>
{
    var orders = await redisService.GetOrdersAsync();
    return Results.Ok(orders);
});

app.MapDelete("/api/orders", async (IRedisService redisService) =>
{
    await redisService.ClearOrdersAsync();
    return Results.Ok("All orders cleared");
});

app.MapPost("/api/order", async (FrietOrder order, IRedisService redisService) =>
{
    await redisService.SaveOrderAsync(order);
    var orders = await redisService.GetOrdersAsync();

    Log.Information($"Order saved: {order}");
    return Results.Ok(orders);
});

// External API endpoints for order management
app.MapPost("/api/order/guest", async (FrietOrder order, IRedisService redisService) =>
{
    try
    {
        // Set items to not need any quantity
        foreach (var orderItem in order.Items)
            orderItem.NeedsQuantity = false;
        
        // Get existing orders
        var existingOrders = await redisService.GetOrdersAsync();
        
        // Find existing order for this user
        var existingOrder = existingOrders.FirstOrDefault(o => o.UserId == order.UserId);
        
        if (existingOrder is not null)
        {
            // Update existing order
            existingOrder.UserName = order.UserName;
            existingOrder.Items = order.Items;
            await redisService.SaveOrderAsync(existingOrder);
            
            Log.Information($"Updated order for user {order.UserId}");
            return Results.Ok(new { message = "Order updated successfully", order = existingOrder });
        }
        
        // Create new order
        order.OrderId = Guid.NewGuid();
        
        await redisService.SaveOrderAsync(order);
        
        Log.Information($"Created new order for user {order.UserId}");
        return Results.Created($"/orders/{order.OrderId}", new { message = "Order created successfully", order });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error processing external order");
        return Results.Problem("Error processing order: " + ex.Message);
    }
});

app.MapGet("/api/orders/{userId}", async (string userId, IRedisService redisService) =>
{
    try
    {
        var orders = await redisService.GetOrdersAsync();
        var userOrder = orders.FirstOrDefault(o => o.UserId == userId);
        
        if (userOrder == null)
        {
            return Results.NotFound($"No order found for user {userId}");
        }
        
        return Results.Ok(userOrder);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error retrieving order for user {UserId}", userId);
        return Results.Problem("Error retrieving order: " + ex.Message);
    }
});

app.MapGet("/api/order/status", (IOrderStatusService orderStatusService) =>
{
    var (isOpen, nextOpening, deadline) = orderStatusService.GetOrderStatus();
    return Results.Ok(new
    {
        isOpen,
        nextOpening,
        deadline
    });
});

app.MapDelete("/api/orders/{userId}", async (string userId, IRedisService redisService) =>
{
    try
    {
        var orders = await redisService.GetOrdersAsync();
        var userOrder = orders.FirstOrDefault(o => o.UserId == userId);
        
        if (userOrder == null)
        {
            return Results.NotFound($"No order found for user {userId}");
        }
        
        orders.Remove(userOrder);
        await redisService.SaveOrderAsync(userOrder);
        
        Log.Information($"Deleted order for user {userId}");
        return Results.Ok(new { message = "Order deleted successfully" });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error deleting order for user {UserId}", userId);
        return Results.Problem("Error deleting order: " + ex.Message);
    }
});

// Test endpoints for Redis
app.MapGet("/redis", async (IRedisService redisService) =>
{
    try
    {
        // Test saving an order
        var testOrder = new FrietOrder
        {
            OrderId = Guid.NewGuid(),
            UserId = "test_user",
            UserName = "Test User",
            Items =
            [
                new OrderItem { Type = "friet", Name = "Large", Quantity = 1 },
                new OrderItem { Type = "snacks", Name = "Frikandel", Quantity = 1 },
                new OrderItem { Type = "extras", Name = "Mayo", Quantity = 1 }
            ]
        };

        await redisService.SaveOrderAsync(testOrder);

        // Test retrieving orders
        var orders = await redisService.GetOrdersAsync();

        // Test clearing orders
        await redisService.ClearOrdersAsync();

        return Results.Ok(new
        {
            Message = "Redis test completed successfully",
            SavedOrder = testOrder,
            RetrievedOrders = orders
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Redis test failed: {ex.Message}");
    }
});

app.MapGet("/redis/connection", async (IConnectionMultiplexer redis) =>
{
    try
    {
        var db = redis.GetDatabase();
        await db.PingAsync();
        return Results.Ok("Redis connection successful");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Redis connection failed: {ex.Message}");
    }
});

// Clear Redis on startup
app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        var redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
        var server = redis.GetServer(redis.GetEndPoints().First());
        await server.FlushDatabaseAsync();
        Console.WriteLine("Redis database cleared on startup");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to clear Redis on startup: {ex.Message}");
    }
});

app.Run();

public class RedisConfig
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int Database { get; set; } = 0;
}