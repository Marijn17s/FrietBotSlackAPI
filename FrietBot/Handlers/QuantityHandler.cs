using SlackNet;
using SlackNet.Events;
using SlackNet.WebApi;
using FrietBot.Services;
using Serilog;
using FrietBot.Models;

namespace FrietBot.Handlers;

public class QuantityHandler : IEventHandler<MessageEvent>
{
    private readonly ISlackApiClient _slack;
    private readonly IRedisService _redisService;

    public QuantityHandler(ISlackApiClient slack, IRedisService redisService)
    {
        _slack = slack;
        _redisService = redisService;
    }

    public async Task Handle(MessageEvent messageEvent)
    {
        try
        {
            // Only handle direct messages
            if (!messageEvent.Channel.StartsWith("D")) return;

            // Get the user's orders
            var orders = await _redisService.GetOrdersAsync();
            var order = orders.FirstOrDefault(o => o.UserId == messageEvent.User && o.CurrentItemId != null);
            
            if (order == null)
            {
                Log.Information("No active order found for user {UserId}", messageEvent.User);
                return;
            }

            // Find the current item
            var currentItem = order.Items.FirstOrDefault(i => i.Type + "_" + i.Name == order.CurrentItemId);
            if (currentItem == null)
            {
                Log.Warning("Current item {CurrentItemId} not found in order {OrderId}", order.CurrentItemId, order.OrderId);
                await _slack.Chat.PostMessage(new Message
                {
                    Channel = messageEvent.Channel,
                    Text = "❌ *Oeps!* Er is iets misgegaan met je bestelling.\n\nStart een nieuwe bestelling via de knop in het vorige bericht."
                });
                return;
            }

            // Handle special cases for removing items
            if (messageEvent.Text == "0" || messageEvent.Text == "-")
            {
                // Remove the current item from the order
                order.Items.Remove(currentItem);
                Log.Information("Removed item {ItemName} from order {OrderId}", currentItem.Name, order.OrderId);

                // Update the order in the orders list
                var orderIndex = orders.FindIndex(o => o.OrderId == order.OrderId);
                if (orderIndex != -1)
                {
                    orders[orderIndex] = order;
                }

                // Check if there are any remaining items
                if (!order.Items.Any())
                {
                    // No items left, inform user and reset order
                    order.CurrentItemId = null;
                    await _slack.Chat.PostMessage(new Message
                    {
                        Channel = messageEvent.Channel,
                        Text = "🗑️ *Bestelling verwijderd*\n\nJe hebt alle items verwijderd. Je kunt een nieuwe bestelling plaatsen met de knop in het vorige bericht."
                    });
                }
                else
                {
                    // Find next item that needs quantity, regardless of category
                    var nextItem = order.Items.FirstOrDefault(i => i.NeedsQuantity);
                    if (nextItem != null)
                    {
                        order.CurrentItemId = nextItem.Type + "_" + nextItem.Name;
                        Log.Information("Moving to next item {ItemName} in order {OrderId}", 
                            nextItem.Name, order.OrderId);
                        
                        await _slack.Chat.PostMessage(new Message
                        {
                            Channel = messageEvent.Channel,
                            Text = $"📝 *Volgende item*\n\nHoeveel {nextItem.Name} wil je bestellen? (1-10)"
                        });
                    }
                    else
                    {
                        // All quantities set, show order summary
                        order.CurrentItemId = null;
                        var summary = $"✅ *Je bestelling*\n\n{string.Join("\n", order.Items.Select(i => $"• {i.Quantity}x {i.Name}"))}";
                        Log.Information("Order {OrderId} completed with items: {Items}", 
                            order.OrderId, string.Join(", ", order.Items.Select(i => $"{i.Quantity}x {i.Name}")));
                        
                        await _slack.Chat.PostMessage(new Message
                        {
                            Channel = messageEvent.Channel,
                            Text = summary
                        });
                    }
                }

                // Save all orders back to Redis
                foreach (var updatedOrder in orders)
                {
                    await _redisService.SaveOrderAsync(updatedOrder);
                }
                return;
            }

            // Try to parse the quantity
            if (!int.TryParse(messageEvent.Text, out int quantity) || quantity < 1 || quantity > 10)
            {
                await _slack.Chat.PostMessage(new Message
                {
                    Channel = messageEvent.Channel,
                    Text = "⚠️ *Ongeldig aantal*\n\nVoer een geldig aantal in tussen 1 en 10."
                });
                return;
            }

            Log.Information("Setting quantity {Quantity} for item {ItemName} in order {OrderId}", 
                quantity, currentItem.Name, order.OrderId);

            currentItem.Quantity = quantity;
            currentItem.NeedsQuantity = false;

            // Update the order in the orders list
            var existingOrderIndex = orders.FindIndex(o => o.OrderId == order.OrderId);
            if (existingOrderIndex != -1)
            {
                orders[existingOrderIndex] = order;
            }

            // Find next item that needs quantity, regardless of category
            var nextQuantityItem = order.Items.FirstOrDefault(i => i.NeedsQuantity);
            if (nextQuantityItem != null)
            {
                order.CurrentItemId = nextQuantityItem.Type + "_" + nextQuantityItem.Name;
                Log.Information("Moving to next item {ItemName} in order {OrderId}", 
                    nextQuantityItem.Name, order.OrderId);
                
                await _slack.Chat.PostMessage(new Message
                {
                    Channel = messageEvent.Channel,
                    Text = $"📝 *Volgende item*\n\nHoeveel {nextQuantityItem.Name} wil je bestellen? (1-10)"
                });
            }
            else
            {
                // All quantities set, show order summary
                order.CurrentItemId = null;
                var summary = $"✅ *Je bestelling*\n\n{string.Join("\n", order.Items.Select(i => $"• {i.Quantity}x {i.Name}"))}";
                Log.Information("Order {OrderId} completed with items: {Items}", 
                    order.OrderId, string.Join(", ", order.Items.Select(i => $"{i.Quantity}x {i.Name}")));
                
                await _slack.Chat.PostMessage(new Message
                {
                    Channel = messageEvent.Channel,
                    Text = summary
                });
            }

            // Save all orders back to Redis
            foreach (var updatedOrder in orders)
            {
                await _redisService.SaveOrderAsync(updatedOrder);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing quantity response");
            await _slack.Chat.PostMessage(new Message
            {
                Channel = messageEvent.Channel,
                Text = "❌ *Er is een fout opgetreden*\n\nEr is een probleem bij het verwerken van je antwoord. Probeer het later opnieuw."
            });
        }
    }
} 