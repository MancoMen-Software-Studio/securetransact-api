using System;
using FluentAssertions;
using SecureTransact.Api.Contracts;
using Xunit;

namespace SecureTransact.Api.Tests.Contracts;

/// <summary>
/// Tests for API contract types.
/// </summary>
public sealed class ApiContractsTests
{
    [Fact]
    public void ProcessTransactionRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        ProcessTransactionRequest request = new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Reference = "Test payment"
        };

        // Assert
        request.SourceAccountId.Should().NotBe(Guid.Empty);
        request.DestinationAccountId.Should().NotBe(Guid.Empty);
        request.Amount.Should().BePositive();
        request.Currency.Should().Be("USD");
        request.Reference.Should().Be("Test payment");
    }

    [Fact]
    public void ReverseTransactionRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        ReverseTransactionRequest request = new()
        {
            Reason = "Customer request"
        };

        // Assert
        request.Reason.Should().Be("Customer request");
    }

    [Fact]
    public void ApiErrorResponse_ShouldContainAllRequiredFields()
    {
        // Arrange & Act
        ApiErrorResponse response = new()
        {
            Code = "Validation.Failed",
            Message = "Validation failed",
            TraceId = "abc123",
            ValidationErrors =
            [
                new ValidationError
                {
                    Field = "Amount",
                    Message = "Amount must be positive"
                }
            ]
        };

        // Assert
        response.Code.Should().Be("Validation.Failed");
        response.Message.Should().Be("Validation failed");
        response.TraceId.Should().Be("abc123");
        response.ValidationErrors.Should().HaveCount(1);
        response.ValidationErrors![0].Field.Should().Be("Amount");
    }

    [Fact]
    public void PaginatedResponse_ShouldCalculateHasMore()
    {
        // Arrange & Act
        PaginatedResponse<string> response1 = new()
        {
            Data = ["a", "b", "c"],
            TotalCount = 10,
            Page = 1,
            PageSize = 3
        };

        PaginatedResponse<string> response2 = new()
        {
            Data = ["a"],
            TotalCount = 3,
            Page = 3,
            PageSize = 1
        };

        // Assert
        response1.HasMore.Should().BeTrue("3 * 1 = 3 < 10");
        response2.HasMore.Should().BeFalse("1 * 3 = 3 >= 3");
    }

    [Fact]
    public void DisputeTransactionRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        DisputeTransactionRequest request = new()
        {
            Reason = "Unauthorized transaction"
        };

        // Assert
        request.Reason.Should().Be("Unauthorized transaction");
    }
}
