using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace SecureTransact.Architecture.Tests;

/// <summary>
/// Tests to verify Clean Architecture layer dependencies.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("SecureTransact.Application")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Domain should not depend on Application. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("SecureTransact.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Domain should not depend on Infrastructure. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Api()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("SecureTransact.Api")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Domain should not depend on Api. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("SecureTransact.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Application should not depend on Infrastructure. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Api()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("SecureTransact.Api")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Application should not depend on Api. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        // Arrange & Act
        TestResult result = Types
            .InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("SecureTransact.Api")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            $"Infrastructure should not depend on Api. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
