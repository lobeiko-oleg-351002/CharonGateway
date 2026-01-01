namespace CharonGateway.Models.Requests;

public class MetricAggregationRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Type { get; set; }
}


