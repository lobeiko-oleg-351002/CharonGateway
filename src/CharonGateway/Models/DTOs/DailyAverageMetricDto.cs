namespace CharonGateway.Models.DTOs;

public class DailyAverageMetricDto
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, double> AverageValues { get; set; } = new();
    public int Count { get; set; }
}




