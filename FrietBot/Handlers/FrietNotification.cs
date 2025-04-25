using SlackNet;
using SlackNet.Events;
using SlackNet.WebApi;
using FrietBot.Services;

namespace FrietBot.Handlers;

public class FrietNotification : IEventHandler<MessageEvent>
{
    private readonly IRedisService _redisService;
    private readonly ISlackApiClient _slackClient;

    public FrietNotification(IRedisService redisService, ISlackApiClient slackClient)
    {
        _redisService = redisService;
        _slackClient = slackClient;
    }

    public async Task Handle(MessageEvent eventMessage)
    {
        if (eventMessage.Text?.ToLower().Contains("ping") is true)
        {
            var orders = await _redisService.GetOrdersAsync();
            
            // Group orders by item name and sum quantities
            var groupedOrders = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.Name)
                .Select(g => new { Name = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .OrderBy(o => o.Name);

            var message = "Current orders:\n";
            foreach (var order in groupedOrders)
            {
                message += $"{order.Quantity}x {order.Name}\n";
            }

            await _slackClient.Chat.PostMessage(new Message
            {
                Channel = eventMessage.Channel,
                Text = message
            });
        }
    }
}