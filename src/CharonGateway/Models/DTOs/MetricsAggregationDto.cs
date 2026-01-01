namespace CharonGateway.Models.DTOs;

public class MetricsAggregationDto
{
    public int TotalCount { get; set; }
    public List<TypeAggregationDto> TypeAggregations { get; set; } = new();
}

public class TypeAggregationDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}


