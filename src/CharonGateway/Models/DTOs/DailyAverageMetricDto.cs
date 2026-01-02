using System.Text.Json.Serialization;

namespace CharonGateway.Models.DTOs;

public class DailyAverageMetricDto
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("averageValues")]
    public Dictionary<string, double> AverageValues { get; set; } = new();
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
}





