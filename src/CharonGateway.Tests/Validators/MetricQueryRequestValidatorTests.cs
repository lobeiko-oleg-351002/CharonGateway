using CharonGateway.Models.Requests;
using CharonGateway.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CharonGateway.Tests.Validators;

public class MetricQueryRequestValidatorTests
{
    private readonly MetricQueryRequestValidator _validator;

    public MetricQueryRequestValidatorTests()
    {
        _validator = new MetricQueryRequestValidator();
    }

    [Fact]
    public void ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var request = new MetricQueryRequest
        {
            Page = 1,
            PageSize = 10,
            SortBy = "CreatedAt",
            SortOrder = "desc"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenPageIsZero()
    {
        // Arrange
        var request = new MetricQueryRequest { Page = 0, PageSize = 10 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void ShouldFail_WhenPageIsNegative()
    {
        // Arrange
        var request = new MetricQueryRequest { Page = -1, PageSize = 10 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void ShouldFail_WhenPageSizeIsZero()
    {
        // Arrange
        var request = new MetricQueryRequest { Page = 1, PageSize = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void ShouldFail_WhenPageSizeIsGreaterThan100()
    {
        // Arrange
        var request = new MetricQueryRequest { Page = 1, PageSize = 101 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void ShouldFail_WhenSortByIsInvalid()
    {
        // Arrange
        var request = new MetricQueryRequest { Page = 1, PageSize = 10, SortBy = "InvalidField" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void ShouldPass_WhenSortByIsValid()
    {
        // Arrange
        var validFields = new[] { "Type", "Name", "CreatedAt", "type", "name", "createdat" };

        foreach (var field in validFields)
        {
            var request = new MetricQueryRequest { Page = 1, PageSize = 10, SortBy = field };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }
    }

    [Fact]
    public void ShouldFail_WhenSortOrderIsInvalid()
    {
        // Arrange
        var request = new MetricQueryRequest { Page = 1, PageSize = 10, SortOrder = "invalid" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }

    [Fact]
    public void ShouldPass_WhenSortOrderIsValid()
    {
        // Arrange
        var validOrders = new[] { "asc", "desc", "ASC", "DESC" };

        foreach (var order in validOrders)
        {
            var request = new MetricQueryRequest { Page = 1, PageSize = 10, SortOrder = order };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
        }
    }

    [Fact]
    public void ShouldFail_WhenToDateIsBeforeFromDate()
    {
        // Arrange
        var request = new MetricQueryRequest
        {
            Page = 1,
            PageSize = 10,
            FromDate = new DateTime(2024, 1, 15),
            ToDate = new DateTime(2024, 1, 10)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ToDate);
    }

    [Fact]
    public void ShouldPass_WhenToDateIsAfterFromDate()
    {
        // Arrange
        var request = new MetricQueryRequest
        {
            Page = 1,
            PageSize = 10,
            FromDate = new DateTime(2024, 1, 10),
            ToDate = new DateTime(2024, 1, 15)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }
}


