using FluentAssertions;
using SecureTransact.Domain.Abstractions;
using Xunit;

namespace SecureTransact.Domain.Tests.Abstractions;

public sealed class DomainErrorTests
{
    [Fact]
    public void None_ShouldHaveEmptyCodeAndDescription()
    {
        // Arrange & Act
        DomainError error = DomainError.None;

        // Assert
        error.Code.Should().BeEmpty();
        error.Description.Should().BeEmpty();
        error.Type.Should().Be(ErrorType.None);
    }

    [Fact]
    public void NullValue_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        DomainError error = DomainError.NullValue;

        // Assert
        error.Code.Should().Be("General.NullValue");
        error.Description.Should().NotBeEmpty();
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Validation_ShouldCreateValidationError()
    {
        // Arrange
        const string code = "Test.InvalidInput";
        const string description = "The input is invalid.";

        // Act
        DomainError error = DomainError.Validation(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void NotFound_ShouldCreateNotFoundError()
    {
        // Arrange
        const string code = "Test.NotFound";
        const string description = "The resource was not found.";

        // Act
        DomainError error = DomainError.NotFound(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Conflict_ShouldCreateConflictError()
    {
        // Arrange
        const string code = "Test.Conflict";
        const string description = "A conflict occurred.";

        // Act
        DomainError error = DomainError.Conflict(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Unauthorized_ShouldCreateUnauthorizedError()
    {
        // Arrange
        const string code = "Test.Unauthorized";
        const string description = "Authentication required.";

        // Act
        DomainError error = DomainError.Unauthorized(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_ShouldCreateForbiddenError()
    {
        // Arrange
        const string code = "Test.Forbidden";
        const string description = "Access denied.";

        // Act
        DomainError error = DomainError.Forbidden(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Failure_ShouldCreateFailureError()
    {
        // Arrange
        const string code = "Test.Failure";
        const string description = "A business rule was violated.";

        // Act
        DomainError error = DomainError.Failure(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void DomainError_ShouldBeEqualWhenPropertiesMatch()
    {
        // Arrange
        DomainError error1 = DomainError.Validation("Test.Code", "Test description");
        DomainError error2 = DomainError.Validation("Test.Code", "Test description");

        // Act & Assert
        error1.Should().Be(error2);
    }

    [Fact]
    public void DomainError_ShouldNotBeEqualWhenPropertiesDiffer()
    {
        // Arrange
        DomainError error1 = DomainError.Validation("Test.Code1", "Test description");
        DomainError error2 = DomainError.Validation("Test.Code2", "Test description");

        // Act & Assert
        error1.Should().NotBe(error2);
    }
}
