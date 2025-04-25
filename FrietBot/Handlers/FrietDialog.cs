using SlackNet;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;
using FrietBot.Services;
using FrietBot.Models;
using Serilog;
using Button = SlackNet.Blocks.Button;

namespace FrietBot.Handlers;

public class FrietDialog : IBlockActionHandler<ButtonAction>, IViewSubmissionHandler
{
    private readonly ISlackApiClient _slack;
    private readonly IRedisService _redisService;
    private readonly IMenuService _menuService;
    private readonly IOrderStatusService _orderStatusService;

    public FrietDialog(ISlackApiClient slack, IRedisService redisService, IMenuService menuService, IOrderStatusService orderStatusService)
    {
        _slack = slack;
        _redisService = redisService;
        _menuService = menuService;
        _orderStatusService = orderStatusService;
    }

    public async Task Handle(ButtonAction action, BlockActionRequest request)
    {
        var (isOpen, nextOpening, deadline) = _orderStatusService.GetOrderStatus();
        
        if (!isOpen)
        {
            var nextOpeningMessage = nextOpening.HasValue
                ? $"De volgende frietdag bestelling opent op {TimeZoneInfo.ConvertTimeFromUtc(nextOpening.Value, TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam")):dddd d MMMM} om {TimeZoneInfo.ConvertTimeFromUtc(nextOpening.Value, TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam")):HH:mm} uur."
                : "Er is momenteel geen frietdag gepland.";
            
            await _slack.Chat.PostMessage(new Message
            {
                Channel = request.User.Id,
                Text = "Frietdag bestelling is nog niet open",
                Blocks = new List<Block>
                {
                    new SectionBlock
                    {
                        Text = new Markdown($"*Frietdag bestelling is nog niet open* 🍟\n\n{nextOpeningMessage}")
                    }
                }
            });
            return;
        }

        if (action.ActionId is "open_friet_dialog")
        {
            await HandleOpenDialog(request);
        }
        else if (action.ActionId is "confirm_order")
        {
            await HandleConfirmOrder(request, action.Value);
        }
        else if (action.ActionId.StartsWith("quantity_"))
        {
            await HandleQuantityUpdate(request, action);
        }
    }

    private async Task HandleOpenDialog(BlockActionRequest request)
    {
        try
        {
            var menuConfig = _menuService.GetMenuConfig();
            var modal = new ModalViewDefinition
            {
                Title = new PlainText("Bestelling plaatsen"),
                Submit = new PlainText("Bevestigen"),
                Close = new PlainText("Annuleren"),
                CallbackId = "friet_order",
                Blocks = new List<Block>
                {
                    new InputBlock
                    {
                        BlockId = "friet_select",
                        Label = new PlainText("Friet"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "friet_selection",
                            Placeholder = new PlainText("Selecteer friet..."),
                            Options = menuConfig.Friet.Select(f => new SlackNet.Blocks.Option { Text = new PlainText(f.Name), Value = f.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "snacks_select",
                        Label = new PlainText("Snacks"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "snacks_selection",
                            Placeholder = new PlainText("Selecteer snacks..."),
                            Options = menuConfig.Snacks.Select(s => new SlackNet.Blocks.Option { Text = new PlainText(s.Name), Value = s.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "burgers_select",
                        Label = new PlainText("Burgers"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "burgers_selection",
                            Placeholder = new PlainText("Selecteer burgers..."),
                            Options = menuConfig.Burgers.Select(b => new SlackNet.Blocks.Option { Text = new PlainText(b.Name), Value = b.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "broodjes_select",
                        Label = new PlainText("Broodjes"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "broodjes_selection",
                            Placeholder = new PlainText("Selecteer broodjes..."),
                            Options = menuConfig.Broodjes.Select(b => new SlackNet.Blocks.Option { Text = new PlainText(b.Name), Value = b.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "veggiesnacks_select",
                        Label = new PlainText("Veggie Snacks"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "veggiesnacks_selection",
                            Placeholder = new PlainText("Selecteer veggie snacks..."),
                            Options = menuConfig.VeggieSnacks.Select(v => new SlackNet.Blocks.Option { Text = new PlainText(v.Name), Value = v.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "schotelsmetsaladesenfrites_select",
                        Label = new PlainText("Schotels met salades en frites"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "schotelsmetsaladesenfrites_selection",
                            Placeholder = new PlainText("Selecteer schotels..."),
                            Options = menuConfig.SchotelsMetSaladesEnFrites.Select(s => new SlackNet.Blocks.Option { Text = new PlainText(s.Name), Value = s.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "schotelsmetsaladeszonderfrites_select",
                        Label = new PlainText("Schotels met salades, zonder frites"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "schotelsmetsaladeszonderfrites_selection",
                            Placeholder = new PlainText("Selecteer schotels..."),
                            Options = menuConfig.SchotelsMetSaladesZonderFrites.Select(s => new SlackNet.Blocks.Option { Text = new PlainText(s.Name), Value = s.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "diversen_select",
                        Label = new PlainText("Diversen"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "diversen_selection",
                            Placeholder = new PlainText("Selecteer diversen..."),
                            Options = menuConfig.Diversen.Select(d => new SlackNet.Blocks.Option { Text = new PlainText(d.Name), Value = d.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "dranken_select",
                        Label = new PlainText("Dranken"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "dranken_selection",
                            Placeholder = new PlainText("Selecteer dranken..."),
                            Options = menuConfig.Dranken.Select(d => new SlackNet.Blocks.Option { Text = new PlainText(d.Name), Value = d.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "warmedranken_select",
                        Label = new PlainText("Warme dranken"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "warmedranken_selection",
                            Placeholder = new PlainText("Selecteer warme dranken..."),
                            Options = menuConfig.WarmeDranken.Select(w => new SlackNet.Blocks.Option { Text = new PlainText(w.Name), Value = w.Id }).ToList()
                        }
                    },
                    new InputBlock
                    {
                        BlockId = "extras_select",
                        Label = new PlainText("Extras"),
                        Optional = true,
                        Element = new StaticMultiSelectMenu
                        {
                            ActionId = "extras_selection",
                            Placeholder = new PlainText("Selecteer extras..."),
                            Options = menuConfig.Extras.Select(e => new SlackNet.Blocks.Option { Text = new PlainText(e.Name), Value = e.Id }).ToList()
                        }
                    }
                }
            };

            await _slack.Views.Open(request.TriggerId, modal);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening order modal");
            await _slack.Chat.PostEphemeral(request.User.Id, new Message
            {
                Channel = request.Channel.Id,
                Text = "Er is een fout opgetreden bij het openen van het bestelformulier. Probeer het later opnieuw."
            });
        }
    }

    private async Task HandleQuantityUpdate(BlockActionRequest request, ButtonAction action)
    {
        try
        {
            // Extract order ID from button value
            var orderId = action.Value;
            var orders = await _redisService.GetOrdersAsync();
            var order = orders.FirstOrDefault(o => o.OrderId.ToString() == orderId);
            if (order is null)
            {
                Log.Error($"Order {orderId} not found");
                await _slack.Chat.PostEphemeral(request.Channel.Id, new Message
                {
                    Text = "Deze bestelling kon niet worden gevonden."
                });
                return;
            }

            // Extract item type, name and action from action ID
            var parts = action.ActionId.Split('_');
            var actionType = parts[1]; // increment or decrement
            var itemType = parts[2];
            var itemName = string.Join("_", parts.Skip(3));

            // Find and update the item
            var item = order.Items.FirstOrDefault(i => i.Type == itemType && i.Name == itemName);
            if (item is null) return;

            // Update quantity based on action
            if (actionType is "increment")
            {
                item.Quantity = Math.Min(item.Quantity + 1, 10); // Max 10
            }
            else if (actionType is "decrement")
            {
                item.Quantity = Math.Max(item.Quantity - 1, 1); // Min 1
            }

            // Save updated order
            await _redisService.SaveOrderAsync(order);

            // Update the message
            var blocks = new List<Block>
            {
                new SectionBlock
                {
                    Text = new Markdown($"*Bestelling voor {order.UserName}*")
                },
                new DividerBlock()
            };

            foreach (var orderItem in order.Items)
            {
                blocks.Add(new SectionBlock
                {
                    Text = new Markdown($"*{orderItem.Name}*"),
                    Accessory = new Button
                    {
                        Text = new PlainText($"{orderItem.Quantity}"),
                        ActionId = $"quantity_display_{orderItem.Type}_{orderItem.Name}",
                        Value = order.OrderId.ToString()
                    }
                });

                blocks.Add(new ActionsBlock
                {
                    Elements = new List<IActionElement>
                    {
                        new Button
                        {
                            Text = new PlainText("-"),
                            ActionId = $"quantity_decrement_{orderItem.Type}_{orderItem.Name}",
                            Value = order.OrderId.ToString()
                        },
                        new Button
                        {
                            Text = new PlainText("+"),
                            ActionId = $"quantity_increment_{orderItem.Type}_{orderItem.Name}",
                            Value = order.OrderId.ToString()
                        }
                    }
                });
            }

            blocks.Add(new ActionsBlock
            {
                Elements = new List<IActionElement>
                {
                    new Button
                    {
                        Text = new PlainText("Bevestig bestelling"),
                        ActionId = "confirm_order",
                        Style = ButtonStyle.Primary,
                        Value = order.OrderId.ToString()
                    }
                }
            });

            await _slack.Chat.Update(new MessageUpdate
            {
                Ts = request.Message.Ts,
                ChannelId = request.Channel.Id,
                Blocks = blocks
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating quantity");
            await _slack.Chat.PostEphemeral(request.Channel.Id, new Message
            {
                Text = "Er is een fout opgetreden bij het bijwerken van het aantal. Probeer het later opnieuw."
            });
        }
    }

    private async Task HandleConfirmOrder(BlockActionRequest request, string orderIdStr)
    {
        try
        {
            var orderId = Guid.Parse(orderIdStr);
            var orders = await _redisService.GetOrdersAsync();
            var order = orders.FirstOrDefault(o => o.OrderId == orderId);

            if (order is null)
            {
                await _slack.Chat.PostEphemeral(request.User.Id, new Message
                {
                    Channel = request.Channel.Id,
                    Text = "Bestelling niet gevonden. Probeer opnieuw te bestellen."
                });
                return;
            }

            // Send confirmation message
            var orderSummary = string.Join("\n", order.Items.Select(i => $"{i.Quantity}x {i.Name}"));
            await _slack.Chat.PostMessage(new Message
            {
                Channel = request.Channel.Id,
                Text = $"Bestelling bevestigd voor {order.UserName}:\n{orderSummary}"
            });

            // Delete the order message
            await _slack.Chat.Delete(request.Message.Ts, request.Channel.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error confirming order");
            await _slack.Chat.PostEphemeral(request.User.Id, new Message
            {
                Channel = request.Channel.Id,
                Text = "Er is een fout opgetreden bij het bevestigen van de bestelling. Probeer het later opnieuw."
            });
        }
    }

    public async Task<ViewSubmissionResponse> Handle(ViewSubmission viewSubmission)
    {
        try
        {
            // Get selected items from each category, making them optional
            var frietSelections = viewSubmission.View.State.Values["friet_select"]?["friet_selection"] as StaticMultiSelectValue;
            var snacksSelections = viewSubmission.View.State.Values["snacks_select"]?["snacks_selection"] as StaticMultiSelectValue;
            var burgersSelections = viewSubmission.View.State.Values["burgers_select"]?["burgers_selection"] as StaticMultiSelectValue;
            var broodjesSelections = viewSubmission.View.State.Values["broodjes_select"]?["broodjes_selection"] as StaticMultiSelectValue;
            var veggieSnacksSelections = viewSubmission.View.State.Values["veggiesnacks_select"]?["veggiesnacks_selection"] as StaticMultiSelectValue;
            var schotelsMetSaladesEnFritesSelections = viewSubmission.View.State.Values["schotelsmetsaladesenfrites_select"]?["schotelsmetsaladesenfrites_selection"] as StaticMultiSelectValue;
            var schotelsMetSaladesZonderFritesSelections = viewSubmission.View.State.Values["schotelsmetsaladeszonderfrites_select"]?["schotelsmetsaladeszonderfrites_selection"] as StaticMultiSelectValue;
            var diversenSelections = viewSubmission.View.State.Values["diversen_select"]?["diversen_selection"] as StaticMultiSelectValue;
            var drankenSelections = viewSubmission.View.State.Values["dranken_select"]?["dranken_selection"] as StaticMultiSelectValue;
            var warmeDrankenSelections = viewSubmission.View.State.Values["warmedranken_select"]?["warmedranken_selection"] as StaticMultiSelectValue;
            var extrasSelections = viewSubmission.View.State.Values["extras_select"]?["extras_selection"] as StaticMultiSelectValue;

            // Get existing orders
            var existingOrders = await _redisService.GetOrdersAsync();
            
            // Find existing order for this user
            var existingOrder = existingOrders.FirstOrDefault(o => o.UserId == viewSubmission.User.Id);
            
            // Create new order or update existing one
            var order = existingOrder ?? new FrietOrder
            {
                OrderId = Guid.NewGuid(),
                UserId = viewSubmission.User.Id,
                UserName = viewSubmission.User.Name,
                Items = [],
            };

            // Clear existing items to replace with new selections
            order.Items.Clear();

            // Add selected items with initial quantity of 1
            if (frietSelections?.SelectedOptions is not null)
            {
                foreach (var option in frietSelections.SelectedOptions)
                {
                    const string type = "friet";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (snacksSelections?.SelectedOptions is not null)
            {
                foreach (var option in snacksSelections.SelectedOptions)
                {
                    const string type = "snacks";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (burgersSelections?.SelectedOptions is not null)
            {
                foreach (var option in burgersSelections.SelectedOptions)
                {
                    const string type = "burgers";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (broodjesSelections?.SelectedOptions is not null)
            {
                foreach (var option in broodjesSelections.SelectedOptions)
                {
                    const string type = "broodjes";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (veggieSnacksSelections?.SelectedOptions is not null)
            {
                foreach (var option in veggieSnacksSelections.SelectedOptions)
                {
                    const string type = "veggie_snacks";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (schotelsMetSaladesEnFritesSelections?.SelectedOptions is not null)
            {
                foreach (var option in schotelsMetSaladesEnFritesSelections.SelectedOptions)
                {
                    const string type = "schotels_met_salades_en_frites";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (schotelsMetSaladesZonderFritesSelections?.SelectedOptions is not null)
            {
                foreach (var option in schotelsMetSaladesZonderFritesSelections.SelectedOptions)
                {
                    const string type = "schotels_met_salades_zonder_frites";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (diversenSelections?.SelectedOptions is not null)
            {
                foreach (var option in diversenSelections.SelectedOptions)
                {
                    const string type = "diversen";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (drankenSelections?.SelectedOptions is not null)
            {
                foreach (var option in drankenSelections.SelectedOptions)
                {
                    const string type = "dranken";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (warmeDrankenSelections?.SelectedOptions is not null)
            {
                foreach (var option in warmeDrankenSelections.SelectedOptions)
                {
                    const string type = "warme_dranken";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            if (extrasSelections?.SelectedOptions is not null)
            {
                foreach (var option in extrasSelections.SelectedOptions)
                {
                    const string type = "extras";
                    var menuItem = _menuService.GetMenuItem(type, option.Value);
                    if (menuItem is not null)
                    {
                        order.Items.Add(new OrderItem {
                            Type = type, 
                            Id = menuItem.Id,
                            Name = menuItem.Name,
                            Quantity = 1, 
                            NeedsQuantity = true 
                        });
                    }
                }
            }

            // Only proceed if at least one item was selected
            if (!order.Items.Any())
            {
                var errors = new Dictionary<string, string>
                {
                    { "friet_select", "Selecteer minimaal één item" }
                };
                var response = new ViewErrorsResponse
                {
                    Errors = errors
                };
                return response;
            }

            // Save the order
            await _redisService.SaveOrderAsync(order);

            // Start the quantity conversation
            var firstItem = order.Items.First();
            order.CurrentItemId = firstItem.Type + "_" + firstItem.Id;
            await _redisService.SaveOrderAsync(order);

            // Send the first quantity question
            await _slack.Chat.PostMessage(new Message
            {
                Channel = viewSubmission.User.Id,
                Text = $"Hoeveel {firstItem.Name} wil je bestellen? (1-10)"
            });

            return ViewSubmissionResponse.Null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing order selection");
            return ViewSubmissionResponse.Null;
        }
    }

    public Task HandleClose(ViewClosed viewClosed)
    {
        return Task.CompletedTask;
    }
}