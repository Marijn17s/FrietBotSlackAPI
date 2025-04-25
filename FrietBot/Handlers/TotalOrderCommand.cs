using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;
using FrietBot.Services;
using Serilog;

namespace FrietBot.Handlers;

public class TotalOrderCommand : ISlashCommandHandler
{
    private readonly IRedisService _redisService;

    public TotalOrderCommand(IRedisService redisService)
    {
        _redisService = redisService;
    }

    public async Task<SlashCommandResponse> Handle(SlashCommand command)
    {
        try
        {
            // Only allow the command in direct messages with the bot
            if (!command.ChannelId.StartsWith("D"))
            {
                return new SlashCommandResponse
                {
                    ResponseType = ResponseType.Ephemeral,
                    Message = new Message
                    {
                        Text = "Dit commando is alleen beschikbaar in een direct bericht met de bot."
                    }
                };
            }

            // Get all orders for today
            var orders = await _redisService.GetOrdersAsync();
            
            if (!orders.Any())
            {
                return new SlashCommandResponse
                {
                    ResponseType = ResponseType.Ephemeral,
                    Message = new Message
                    {
                        Text = "Er zijn nog geen bestellingen geplaatst voor vandaag."
                    }
                };
            }

            // Group orders by type and name
            var groupedOrders = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => new { i.Type, i.Name })
                .Select(g => new { g.Key.Type, g.Key.Name, Quantity = g.Sum(i => i.Quantity) })
                .GroupBy(i => i.Type)
                .OrderBy(g => g.Key);

            // Build the message blocks
            var blocks = new List<Block>
            {
                new HeaderBlock
                {
                    Text = new PlainText("🍟 *Totale bestelling tot nu toe* 🍟")
                }
            };

            // Add each category and its items
            var totalItems = 0;
            foreach (var category in groupedOrders)
            {
                var categoryName = category.Key switch
                {
                    "friet" => "Friet",
                    "snacks" => "Snacks",
                    "burgers" => "Burgers",
                    "broodjes" => "Broodjes",
                    "veggie_snack" => "Veggie Snacks",
                    "schotel_met_salades_en_frites" => "Schotels met salades en frites",
                    "schotel_met_salades_zonder_frites" => "Schotels met salades, zonder frites",
                    "diversen" => "Diversen",
                    "dranken" => "Dranken",
                    "warme_dranken" => "Warme dranken",
                    "extras" => "Extra",
                    _ => category.Key
                };

                var items = category
                    .OrderBy(i => i.Name)
                    .Select(i => $"{i.Quantity}x {i.Name}")
                    .ToList();

                if (items.Any())
                {
                    blocks.Add(new SectionBlock
                    {
                        Text = new Markdown($"*{categoryName}:*\n{string.Join("\n", items)}")
                    });
                    totalItems += category.Sum(i => i.Quantity);
                }
            }

            // Add total count
            blocks.Add(new SectionBlock
            {
                Text = new Markdown($"\n*Totaal aantal items:* {totalItems}")
            });

            return new SlashCommandResponse
            {
                ResponseType = ResponseType.Ephemeral,
                Message = new Message
                {
                    Text = "🍟 *Totale bestelling tot nu toe* 🍟",
                    Blocks = blocks
                }
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing total order command");
            return new SlashCommandResponse
            {
                ResponseType = ResponseType.Ephemeral,
                Message = new Message
                {
                    Text = "Er is een fout opgetreden bij het ophalen van de bestellingen. Probeer het later opnieuw."
                }
            };
        }
    }
} 