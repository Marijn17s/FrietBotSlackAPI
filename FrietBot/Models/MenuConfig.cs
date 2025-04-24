using System.Text.Json.Serialization;

namespace FrietBot.Models;

public class MenuConfig
{
    [JsonPropertyName("friet")]
    public List<MenuItem> Friet { get; set; } = [];
    [JsonPropertyName("snacks")]
    public List<MenuItem> Snacks { get; set; } = [];
    [JsonPropertyName("burgers")]
    public List<MenuItem> Burgers { get; set; } = [];
    [JsonPropertyName("broodjes")]
    public List<MenuItem> Broodjes { get; set; } = [];
    [JsonPropertyName("veggie_snacks")]
    public List<MenuItem> VeggieSnacks { get; set; } = [];
    [JsonPropertyName("schotels_met_salades_en_frites")]
    public List<MenuItem> SchotelsMetSaladesEnFrites { get; set; } = [];
    [JsonPropertyName("schotels_met_salades_zonder_frites")]
    public List<MenuItem> SchotelsMetSaladesZonderFrites { get; set; } = [];
    [JsonPropertyName("diversen")]
    public List<MenuItem> Diversen { get; set; } = [];
    [JsonPropertyName("dranken")]
    public List<MenuItem> Dranken { get; set; } = [];
    [JsonPropertyName("warme_dranken")]
    public List<MenuItem> WarmeDranken { get; set; } = [];
    [JsonPropertyName("extras")]
    public List<MenuItem> Extras { get; set; } = [];
}

public class MenuItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
} 