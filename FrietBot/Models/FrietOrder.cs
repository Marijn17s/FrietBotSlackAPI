using System.Text.Json.Serialization;

namespace FrietBot.Models;

public class FrietOrder
{
    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = [];

    [JsonPropertyName("current_item_id")]
    public string? CurrentItemId { get; set; } // Tracks which item we're currently asking quantity for
}

public class OrderItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // The ID of the menu item (e.g. "friet_speciaal")

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "friet", "snacks", "burgers", "extras"

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("needs_quantity")]
    public bool NeedsQuantity { get; set; } = true; // Whether we still need to ask for quantity
}