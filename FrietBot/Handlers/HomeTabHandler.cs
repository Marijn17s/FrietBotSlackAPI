using SlackNet;
using SlackNet.Events;
using SlackNet.WebApi;
using SlackNet.Blocks;
using Serilog;
using FrietBot.Services;
using FrietBot.Models;

namespace FrietBot.Handlers;

public class HomeTabHandler : IEventHandler<AppHomeOpened>
{
    private readonly ISlackApiClient _slack;
    private readonly IMenuService _menuService;

    public HomeTabHandler(ISlackApiClient slack, IMenuService menuService)
    {
        _slack = slack;
        _menuService = menuService;
    }

    public async Task Handle(AppHomeOpened @event)
    {
        try
        {
            var menuConfig = _menuService.GetMenuConfig();
            var blocks = new List<Block>
            {
                new HeaderBlock
                {
                    Text = new PlainText("👋 Welkom bij FrietBot!")
                },
                new SectionBlock
                {
                    Text = new Markdown("*Hoe werkt het?*\n" +
                        "1️⃣ Klik op de `Bestelling doorgeven` knop hieronder\n" +
                        "2️⃣ Selecteer je gewenste items uit het menu\n" +
                        "3️⃣ Geef aan hoeveel je van elk item wilt\n" +
                        "4️⃣ Bevestig je bestelling")
                },
                new ActionsBlock
                {
                    Elements = new List<IActionElement>
                    {
                        new Button
                        {
                            Text = new PlainText("🍽️ Bestelling doorgeven"),
                            ActionId = "open_friet_dialog",
                            Style = ButtonStyle.Primary
                        }
                    }
                },
                new DividerBlock(),
                new HeaderBlock
                {
                    Text = new PlainText("🍟 Menu Overzicht")
                },
                new SectionBlock
                {
                    Text = new Markdown("Hieronder vind je ons complete menu. Je kunt ook direct bestellen via de knoppen.")
                }
            };

            // Add each category with its items in a more efficient way
            var categories = new Dictionary<string, List<MenuItem>>
            {
                { "Friet", menuConfig.Friet },
                { "Snacks", menuConfig.Snacks },
                { "Burgers", menuConfig.Burgers },
                { "Broodjes", menuConfig.Broodjes },
                { "Veggie Snacks", menuConfig.VeggieSnacks },
                { "Schotels met salades en frites", menuConfig.SchotelsMetSaladesEnFrites },
                { "Schotels met salades, zonder frites", menuConfig.SchotelsMetSaladesZonderFrites },
                { "Diversen", menuConfig.Diversen },
                { "Dranken", menuConfig.Dranken },
                { "Warme dranken", menuConfig.WarmeDranken },
                { "Extra", menuConfig.Extras }
            };

            // Group categories into pairs
            var categoryPairs = categories
                .Where(c => c.Value.Any())
                .Select((category, index) => new { category, index })
                .GroupBy(x => x.index / 2)
                .Select(g => g.Select(x => x.category).ToList())
                .ToList();

            foreach (var pair in categoryPairs)
            {
                if (pair.Count == 1)
                {
                    // Single category
                    var category = pair[0];
                    var itemsText = string.Join("\n", category.Value.Select(item => $"• {item.Name}"));
                    blocks.Add(new SectionBlock
                    {
                        Text = new Markdown($"*{GetCategoryEmoji(category.Key)} {category.Key}*\n{itemsText}"),
                        Accessory = new Button
                        {
                            Text = new PlainText("Bestel nu"),
                            ActionId = "order_now"
                        }
                    });
                }
                else
                {
                    // Two categories side by side
                    var leftCategory = pair[0];
                    var rightCategory = pair[1];
                    
                    var leftItemsText = string.Join("\n", leftCategory.Value.Select(item => $"• {item.Name}"));
                    var rightItemsText = string.Join("\n", rightCategory.Value.Select(item => $"• {item.Name}"));

                    // Create a section block with the left category as main text and right category as accessory
                    blocks.Add(new SectionBlock
                    {
                        Text = new Markdown($"*{GetCategoryEmoji(leftCategory.Key)} {leftCategory.Key}*\n{leftItemsText}"),
                        Accessory = new Button
                        {
                            Text = new PlainText("Bestel nu"),
                            ActionId = "order_now"
                        }
                    });
                }

                blocks.Add(new DividerBlock());
            }

            // Add helpful tips at the bottom
            // blocks.Add(new DividerBlock());
            blocks.Add(new SectionBlock
            {
                Text = new Markdown("*💡 Handige tips:*\n" +
                    "• Gebruik `/bestelling` om de huidige totale bestelling te bekijken\n" +
                    "• Je kunt je bestelling aanpassen tot het sluitingstijdstip door opnieuw je bestelling te versturen")
            });

            var view = new HomeViewDefinition
            {
                Blocks = blocks
            };

            await _slack.Views.Publish(@event.User, view);

            Log.Information("Home tab updated for user {UserId}", @event.User);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating home tab for user {UserId}", @event.User);
        }
    }

    private string GetCategoryEmoji(string categoryName)
    {
        return categoryName.ToLower() switch
        {
            "friet" => "🍟",
            "snacks" => "🌭",
            "burgers" => "🍔",
            "broodjes" => "🥪",
            "veggie snacks" => "🥗",
            "schotels met salades en frites" => "🥙",
            "schotels met salades, zonder frites" => "🥙",
            "diversen" => "🍽️",
            "dranken" => "🥤",
            "warme dranken" => "☕",
            "extra" => "➕",
            _ => "🍽️"
        };
    }
} 