using CharonDbContext.Models;
using HotChocolate.Types;
using System.Text.Json;

namespace CharonGateway.GraphQL.Types;

public class MetricType : ObjectType<Metric>
{
    protected override void Configure(IObjectTypeDescriptor<Metric> descriptor)
    {
        descriptor
            .Field(m => m.PayloadJson)
            .Name("payload")
            .Resolve(context =>
            {
                var json = context.Parent<Metric>().PayloadJson;
                if (string.IsNullOrEmpty(json))
                {
                    return new Dictionary<string, object>();
                }

                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                        ?? new Dictionary<string, object>();
                }
                catch
                {
                    return new Dictionary<string, object>();
                }
            })
            .Type<AnyType>();
    }
}

