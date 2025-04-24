using System.Text.Json;
using StackExchange.Redis;
using FrietBot.Models;
using Serilog;

namespace FrietBot.Services;

public interface IRedisService
{
    Task SaveOrderAsync(FrietOrder order);
    Task<List<FrietOrder>> GetOrdersAsync();
    Task ClearOrdersAsync();
}

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private const string OrdersKey = "friet_orders";

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis;
    }

    public async Task SaveOrderAsync(FrietOrder order)
    {
        try
        {
            var db = _redis.GetDatabase();
            var existingOrders = await GetOrdersAsync();
            
            // Find and update the existing order if it exists
            var existingOrder = existingOrders.FirstOrDefault(o => o.OrderId == order.OrderId);
            if (existingOrder != null)
            {
                existingOrders.Remove(existingOrder);
            }
            
            existingOrders.Add(order);
            
            var serializedOrders = JsonSerializer.Serialize(existingOrders);
            await db.StringSetAsync(OrdersKey, serializedOrders);
            
            Log.Information("Successfully saved order {OrderId} for user {UserId}", order.OrderId, order.UserId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving order {OrderId} to Redis", order.OrderId);
            throw;
        }
    }

    public async Task<List<FrietOrder>> GetOrdersAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            var orders = await db.StringGetAsync(OrdersKey);
            
            if (orders.IsNullOrEmpty)
            {
                return [];
            }
            
            var deserializedOrders = JsonSerializer.Deserialize<List<FrietOrder>>(orders.ToString());
            return deserializedOrders ?? [];
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving orders from Redis");
            return [];
        }
    }

    public async Task ClearOrdersAsync()
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(OrdersKey, JsonSerializer.Serialize(new List<FrietOrder>()));
    }
} 