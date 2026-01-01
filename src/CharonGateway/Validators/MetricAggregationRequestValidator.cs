using CharonGateway.Models.Requests;
using FluentValidation;

namespace CharonGateway.Validators;

public class MetricAggregationRequestValidator : AbstractValidator<MetricAggregationRequest>
{
    public MetricAggregationRequestValidator()
    {
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be greater than or equal to FromDate");
    }
}


