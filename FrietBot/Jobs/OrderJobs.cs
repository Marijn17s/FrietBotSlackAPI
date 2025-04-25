using Quartz;
using Quartz.Spi;
using Serilog;
using SlackNet;
using SlackNet.WebApi;
using SlackNet.Blocks;
using FrietBot.Services;
using FrietBot.Config;

namespace FrietBot.Jobs;

public class OrderJob : IJob
{
    private ISlackService _slackService;

    public OrderJob(ISlackService slackService)
    {
        _slackService = slackService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Log.Information($"Executing Slack message job at: {DateTime.Now}");
        await _slackService.SendOrderRequestToAllUsers();
    }

    public void SetSlackService(ISlackService slackService)
    {
        _slackService = slackService;
    }
}

public class ClearOrdersJob : IJob
{
    private IRedisService _redisService;
    private IOrderStatusService _orderStatusService;

    public ClearOrdersJob(IRedisService redisService, IOrderStatusService orderStatusService)
    {
        _redisService = redisService;
        _orderStatusService = orderStatusService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Log.Information($"Executing clear orders job at: {DateTime.Now}");
        await _redisService.ClearOrdersAsync();
        _orderStatusService.CloseOrdering();
        Log.Information("All orders cleared from Redis and ordering closed");
    }

    public void SetRedisService(IRedisService redisService)
    {
        _redisService = redisService;
    }

    public void SetOrderStatusService(IOrderStatusService orderStatusService)
    {
        _orderStatusService = orderStatusService;
    }
}

public class CloseOrdersJob : IJob
{
    private ISlackService _slackService;

    public CloseOrdersJob(ISlackService slackService)
    {
        _slackService = slackService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Log.Information($"Executing close orders job at: {DateTime.Now}");
        await _slackService.SendTotalOrderMessage();
        Log.Information("Orders are now closed for the week");
    }

    public void SetSlackService(ISlackService slackService)
    {
        _slackService = slackService;
    }
}

public class ResetCycleJob : IJob
{
    private IOrderStatusService _orderStatusService;

    public ResetCycleJob(IOrderStatusService orderStatusService)
    {
        _orderStatusService = orderStatusService;
    }

    public Task Execute(IJobExecutionContext context)
    {
        Log.Information($"Executing reset cycle job at: {DateTime.Now}");
        _orderStatusService.ResetCycle();

        var nextOpening = _orderStatusService.GetOrderStatus().NextOpening;
        Log.Information($"Cycle reset for {nextOpening}");
        
        return Task.CompletedTask;
    }

    public void SetOrderStatusService(IOrderStatusService orderStatusService)
    {
        _orderStatusService = orderStatusService;
    }
}

public interface ISlackService
{
    Task SendOrderRequestToAllUsers();
    Task SendTotalOrderMessage();
}

public class SlackService : ISlackService
{
    private readonly ISlackApiClient _slackClient;
    private readonly IRedisService _redisService;

    public SlackService(ISlackApiClient slackClient, IRedisService redisService)
    {
        _slackClient = slackClient;
        _redisService = redisService;
    }

    public async Task SendOrderRequestToAllUsers()
    {
        var message = new Message
        {
            Text = "🍟 *Frietdag!* 🍟\n\nHet is weer tijd voor friet! Klik op de knop hieronder om je bestelling door te geven.",
            Blocks = new List<Block>
            {
                new SectionBlock
                {
                    Text = new Markdown($"🍟 *Frietdag!* 🍟\n\nHet is weer tijd voor friet! Klik op de knop hieronder om je bestelling door te geven of gebruik deze <{OrderConfig.OrderingLink}|link>")
                },
                new ActionsBlock
                {
                    Elements = new List<IActionElement>
                    {
                        new Button
                        {
                            Text = "🍽️ Bestelling doorgeven",
                            ActionId = "open_friet_dialog",
                            Style = ButtonStyle.Primary
                        }
                    }
                }
            }
        };

        var users = await _slackClient.Users.List();
        foreach (var user in users.Members)
        {
            if (user.IsBot || user.Deleted || user.Name.ToLowerInvariant() is "slackbot") continue;
            
            message.Channel = user.Id;
            await _slackClient.Chat.PostMessage(message);
            
            await Task.Delay(100);
        }
        
        Log.Information("Friet notification sent to all users");
    }
    
    public async Task SendTotalOrderMessage()
    {
        var orders = await _redisService.GetOrdersAsync();
        
        if (!orders.Any())
        {
            var noOrdersMessage = new Message
            {
                Channel = "friet-bestelling",
                Text = "🍟 *Frietdag Bestelling* 🍟",
                Blocks = new List<Block>
                {
                    new HeaderBlock
                    {
                        Text = new PlainText("🍟 *Frietdag Bestelling* 🍟")
                    },
                    new SectionBlock
                    {
                        Text = new Markdown("Er zijn vandaag geen bestellingen geplaatst. 😢")
                    }
                }
            };

            await _slackClient.Chat.PostMessage(noOrdersMessage);
            Log.Information("No orders message sent to friet-bestelling channel");
            return;
        }

        // Group orders by type
        var frietOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "friet"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var snacksOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "snacks"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var burgerOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "burgers"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var broodjesOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "broodjes"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var veggieSnacksOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "veggie_snacks"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var schotelsMetSaladesEnFritesOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "schotels_met_salades_en_frites"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var schotelsMetSaladesZonderFritesOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "schotels_met_salades_zonder_frites"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var diversenOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "diversen"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var drankenOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "dranken"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var warmeDrankenOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "warme_dranken"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();
        var extraOrders = orders.SelectMany(o => o.Items.Where(i => i.Type is "extras"))
            .GroupBy(i => i.Id)
            .Select(g => $"• {g.Sum(i => i.Quantity)}x {g.First().Name}").ToList();

        var message = new Message
        {
            Channel = "friet-bestelling",
            Text = "🍟 *Frietdag Bestelling* 🍟",
            Blocks = new List<Block>
            {
                new HeaderBlock
                {
                    Text = new PlainText("🍟 *Frietdag Bestelling* 🍟")
                }
            }
        };

        // Add sections if there are any orders
        if (frietOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("*🍟 Friet:*\n" + string.Join("\n", frietOrders))
            });
        }

        if (snacksOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🌭 Snacks:*\n" + string.Join("\n", snacksOrders))
            });
        }

        if (burgerOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🍔 Burgers:*\n" + string.Join("\n", burgerOrders))
            });
        }

        if (broodjesOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🥪 Broodjes:*\n" + string.Join("\n", broodjesOrders))
            });
        }

        if (veggieSnacksOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🥗 Veggie Snacks:*\n" + string.Join("\n", veggieSnacksOrders))
            });
        }

        if (schotelsMetSaladesEnFritesOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🥙 Schotels met salades en frites:*\n" + string.Join("\n", schotelsMetSaladesEnFritesOrders))
            });
        }

        if (schotelsMetSaladesZonderFritesOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🥙 Schotels met salades, zonder frites:*\n" + string.Join("\n", schotelsMetSaladesZonderFritesOrders))
            });
        }

        if (diversenOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🍽️ Diversen:*\n" + string.Join("\n", diversenOrders))
            });
        }

        if (drankenOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*🥤 Dranken:*\n" + string.Join("\n", drankenOrders))
            });
        }

        if (warmeDrankenOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*☕ Warme dranken:*\n" + string.Join("\n", warmeDrankenOrders))
            });
        }

        if (extraOrders.Any())
        {
            message.Blocks.Add(new SectionBlock
            {
                Text = new Markdown("\n*➕ Extra:*\n" + string.Join("\n", extraOrders))
            });
        }

        await _slackClient.Chat.PostMessage(message);
        Log.Information("Total order message sent to friet-bestelling channel");
    }
}

public class SchedulerService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IServiceProvider _serviceProvider;
    private IScheduler? _scheduler;

    public SchedulerService(ISchedulerFactory schedulerFactory, IServiceProvider serviceProvider)
    {
        _schedulerFactory = schedulerFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync()
    {
        _scheduler = await _schedulerFactory.GetScheduler();
        
        // Set the job factory on the scheduler
        var jobFactory = _serviceProvider.GetRequiredService<IJobFactory>();
        _scheduler.JobFactory = jobFactory;
        
        // Set up the order request job (base time)
        var orderRequestJobDataMap = new JobDataMap();
        orderRequestJobDataMap.Put("ServiceProvider", _serviceProvider);
        orderRequestJobDataMap.Put("SlackService", _serviceProvider.GetRequiredService<ISlackService>());
        
        var orderRequestJob = JobBuilder.Create<OrderJob>()
            .WithIdentity("fridaySlackMessageJob", "slackJobs")
            .UsingJobData(orderRequestJobDataMap)
            .Build();

        // Calculate the next occurrence of the base time
        var baseTrigger = TriggerBuilder.Create()
            .WithIdentity("frietDagSlackMessageTrigger", "slackTriggers")
            .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(OrderConfig.OrderDay, OrderConfig.OrderHour, OrderConfig.OrderMinute))
            .Build();

        // Schedule the base job first
        await _scheduler.ScheduleJob(orderRequestJob, baseTrigger);

        // Get the next fire time of the base trigger
        var nextFireTime = await _scheduler.GetTrigger(baseTrigger.Key);
        var baseTime = nextFireTime?.GetNextFireTimeUtc()?.UtcDateTime ?? throw new InvalidOperationException("Could not determine next fire time");

        // Set up the close orders job (base + configured offset)
        var closeOrdersJobDataMap = new JobDataMap();
        closeOrdersJobDataMap.Put("ServiceProvider", _serviceProvider);
        closeOrdersJobDataMap.Put("OrderStatusService", _serviceProvider.GetRequiredService<IOrderStatusService>());
        closeOrdersJobDataMap.Put("SlackService", _serviceProvider.GetRequiredService<ISlackService>());
        
        var closeOrdersJob = JobBuilder.Create<CloseOrdersJob>()
            .WithIdentity("closeOrdersJob", "slackJobs")
            .UsingJobData(closeOrdersJobDataMap)
            .Build();

        var closeOrdersTrigger = TriggerBuilder.Create()
            .WithIdentity("closeOrdersTrigger", "slackTriggers")
            .StartAt(baseTime.AddHours(OrderConfig.Offsets.CloseOrdersHours).AddMinutes(OrderConfig.Offsets.CloseOrdersMinutes))
            .WithSimpleSchedule(x => x
                .WithIntervalInHours(24 * 7) // Weekly
                .RepeatForever())
            .Build();

        // Set up the reset cycle job (base + configured offset)
        var resetCycleJobDataMap = new JobDataMap();
        resetCycleJobDataMap.Put("ServiceProvider", _serviceProvider);
        resetCycleJobDataMap.Put("OrderStatusService", _serviceProvider.GetRequiredService<IOrderStatusService>());
        
        var resetCycleJob = JobBuilder.Create<ResetCycleJob>()
            .WithIdentity("resetCycleJob", "slackJobs")
            .UsingJobData(resetCycleJobDataMap)
            .Build();

        var resetCycleTrigger = TriggerBuilder.Create()
            .WithIdentity("resetCycleTrigger", "slackTriggers")
            .StartAt(baseTime.AddHours(OrderConfig.Offsets.ResetCycleHours).AddMinutes(OrderConfig.Offsets.ResetCycleMinutes))
            .WithSimpleSchedule(x => x
                .WithIntervalInHours(24 * 7) // Weekly
                .RepeatForever())
            .Build();

        // Set up the clear orders job (base + configured offset)
        var clearOrdersJobDataMap = new JobDataMap();
        clearOrdersJobDataMap.Put("ServiceProvider", _serviceProvider);
        clearOrdersJobDataMap.Put("RedisService", _serviceProvider.GetRequiredService<IRedisService>());
        
        var clearOrdersJob = JobBuilder.Create<ClearOrdersJob>()
            .WithIdentity("clearOrdersJob", "slackJobs")
            .UsingJobData(clearOrdersJobDataMap)
            .Build();

        var clearOrdersTrigger = TriggerBuilder.Create()
            .WithIdentity("clearOrdersTrigger", "slackTriggers")
            .StartAt(baseTime.AddHours(OrderConfig.Offsets.ClearOrdersHours).AddMinutes(OrderConfig.Offsets.ClearOrdersMinutes))
            .WithSimpleSchedule(x => x
                .WithIntervalInHours(24 * 7) // Weekly
                .RepeatForever())
            .Build();

        // Schedule the remaining jobs
        await _scheduler.ScheduleJob(closeOrdersJob, closeOrdersTrigger);
        await _scheduler.ScheduleJob(resetCycleJob, resetCycleTrigger);
        await _scheduler.ScheduleJob(clearOrdersJob, clearOrdersTrigger);
        await _scheduler.Start();
        
        Log.Information($"Scheduler started. Base time: {baseTime:HH:mm}, " +
                       $"Close orders: +{OrderConfig.Offsets.CloseOrdersHours}h{OrderConfig.Offsets.CloseOrdersMinutes}m, " +
                       $"Reset cycle: +{OrderConfig.Offsets.ResetCycleHours}h{OrderConfig.Offsets.ResetCycleMinutes}m, " +
                       $"Clear orders: +{OrderConfig.Offsets.ClearOrdersHours}h{OrderConfig.Offsets.ClearOrdersMinutes}m");
    }

    public async Task StopAsync()
    {
        if (_scheduler is not null)
        {
            await _scheduler.Shutdown();
            Log.Information("Scheduler stopped");
        }
    }
}

public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;
        var jobDataMap = bundle.JobDetail.JobDataMap;

        // Create the job instance
        if (_serviceProvider.GetService(jobType) is not IJob job)
            throw new SchedulerException($"Failed to create job of type {jobType}");

        // Inject dependencies using pattern matching and null checking
        switch (job)
        {
            case OrderJob orderJob:
                if (jobDataMap.Get("SlackService") is not ISlackService slackService)
                    throw new SchedulerException("SlackService not found in JobDataMap for OrderJob");
                orderJob.SetSlackService(slackService);
                break;
                
            case CloseOrdersJob closeOrdersJob:
                if (jobDataMap.Get("SlackService") is not ISlackService closeOrdersSlackService)
                    throw new SchedulerException("SlackService not found in JobDataMap for CloseOrdersJob");
                closeOrdersJob.SetSlackService(closeOrdersSlackService);
                break;
                
            case ResetCycleJob resetCycleJob:
                if (jobDataMap.Get("OrderStatusService") is not IOrderStatusService orderStatusService)
                    throw new SchedulerException("OrderStatusService not found in JobDataMap for ResetCycleJob");
                resetCycleJob.SetOrderStatusService(orderStatusService);
                break;
                
            case ClearOrdersJob clearOrdersJob:
                if (jobDataMap.Get("OrderStatusService") is not IOrderStatusService clearOrderStatusService)
                    throw new SchedulerException("OrderStatusService not found in JobDataMap for ClearOrdersJob");
                clearOrdersJob.SetOrderStatusService(clearOrderStatusService);

                if (jobDataMap.Get("RedisService") is not IRedisService redisService)
                    throw new SchedulerException("RedisService not found in JobDataMap for ClearOrdersJob");
                clearOrdersJob.SetRedisService(redisService);
                break;
        }

        return job;
    }

    public void ReturnJob(IJob job)
    {
        (job as IDisposable)?.Dispose();
    }
}