using CharonGateway.Models.Requests;
using FluentValidation;

namespace CharonGateway.Validators;

public class MetricQueryRequestValidator : AbstractValidator<MetricQueryRequest>
{
    public MetricQueryRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than zero");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrEmpty(x.SortBy))
            .WithMessage("SortBy must be one of: Type, Name, CreatedAt");

        RuleFor(x => x.SortOrder)
            .Must(BeValidSortOrder)
            .When(x => !string.IsNullOrEmpty(x.SortOrder))
            .WithMessage("SortOrder must be 'asc' or 'desc'");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be greater than or equal to FromDate");
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy))
            return true;

        var validFields = new[] { "type", "name", "createdat" };
        return validFields.Contains(sortBy.ToLower());
    }

    private static bool BeValidSortOrder(string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortOrder))
            return true;

        var validOrders = new[] { "asc", "desc" };
        return validOrders.Contains(sortOrder.ToLower());
    }
}


