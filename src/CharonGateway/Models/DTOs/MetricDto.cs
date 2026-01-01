namespace CharonGateway.Models.DTOs;

public class MetricDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}


