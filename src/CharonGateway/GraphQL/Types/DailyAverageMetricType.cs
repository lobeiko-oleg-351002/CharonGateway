using CharonGateway.Models.DTOs;
using HotChocolate.Types;

namespace CharonGateway.GraphQL.Types;

public class DailyAverageMetricType : ObjectType<DailyAverageMetricDto>
{
    protected override void Configure(IObjectTypeDescriptor<DailyAverageMetricDto> descriptor)
    {
        descriptor
            .Field(d => d.AverageValues)
            .Type<AnyType>()
            .Description("Average values for numeric fields (limited to top 5 most common keys)");
        
        // Reduce complexity by making fields simpler
        descriptor
            .Field(d => d.Count)
            .Description("Number of metrics aggregated");
    }
}

