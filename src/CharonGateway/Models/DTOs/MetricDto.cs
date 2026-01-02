using System.Text.Json.Serialization;

namespace CharonGateway.Models.DTOs;

public class MetricDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public Dictionary<string, object> Payload { get; set; } = new();
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}




