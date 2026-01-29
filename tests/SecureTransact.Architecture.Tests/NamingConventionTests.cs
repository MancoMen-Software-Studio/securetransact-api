using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace SecureTransact.Architecture.Tests;

/// <summary>
/// Tests to verify naming conventions are followed.
/// </summary>
public sealed class NamingConventionTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;

    [Fact]
    public void DomainEvents_ShouldEndWith_Event()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(Domain.Abstractions.IDomainEvent))
            .And()
            .AreNotInterfaces()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Event")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Domain events should end with 'Event'. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void CommandHandlers_ShouldEndWith_Handler()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Command handlers should end with 'Handler'. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void QueryHandlers_ShouldEndWith_Handler()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Query handlers should end with 'Handler'. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Validators_ShouldEndWith_Validator()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Validators should end with 'Validator'. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Interfaces_ShouldStartWith_I()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Interfaces should start with 'I'. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void ValueObjects_ShouldBeSealed()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("SecureTransact.Domain.ValueObjects")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Value objects should be sealed. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
