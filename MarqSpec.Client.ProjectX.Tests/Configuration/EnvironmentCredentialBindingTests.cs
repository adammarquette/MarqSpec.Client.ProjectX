using System;
using System.Collections.Generic;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MarqSpec.Client.ProjectX.Tests.Configuration;

/// <summary>
/// Credentials must arrive from either environment-variable convention (R-1.2).
/// </summary>
/// <remarks>
/// The double-underscore form is the ASP.NET convention and is what <c>release.yml</c> sets. It normally
/// reaches the options through the host's environment configuration provider — but
/// <c>AddProjectXApiClient</c> accepts an arbitrary <see cref="IConfiguration"/>, and one built without that
/// provider never sees it. The workflow was therefore setting credentials the client could not read.
/// </remarks>
public class EnvironmentCredentialBindingTests : IDisposable
{
    private static readonly string[] _variables =
    [
        "PROJECTX_API_KEY",
        "PROJECTX_API_SECRET",
        "ProjectX__ApiKey",
        "ProjectX__ApiSecret",
    ];

    public EnvironmentCredentialBindingTests() => ClearVariables();

    public void Dispose()
    {
        ClearVariables();
        GC.SuppressFinalize(this);
    }

    private static void ClearVariables()
    {
        foreach (var name in _variables)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    /// <summary>
    /// An <see cref="IConfiguration"/> with no environment-variable provider, which is what makes this a real
    /// gap rather than a theoretical one.
    /// </summary>
    private static ProjectXOptions Resolve()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        return new ServiceCollection()
            .AddProjectXApiClient(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ProjectXOptions>>()
            .Value;
    }

    [Fact]
    public void AddProjectXApiClient_ShouldBindCredentials_WhenDoubleUnderscoreVariablesAreSet()
    {
        Environment.SetEnvironmentVariable("ProjectX__ApiKey", "double-underscore-key");
        Environment.SetEnvironmentVariable("ProjectX__ApiSecret", "double-underscore-secret");

        var options = Resolve();

        options.ApiKey.Should().Be("double-underscore-key",
            "ProjectX__ApiKey is the ASP.NET convention and is what release.yml sets");
        options.ApiSecret.Should().Be("double-underscore-secret");
    }

    [Fact]
    public void AddProjectXApiClient_ShouldBindCredentials_WhenLegacyFlatVariablesAreSet()
    {
        Environment.SetEnvironmentVariable("PROJECTX_API_KEY", "flat-key");
        Environment.SetEnvironmentVariable("PROJECTX_API_SECRET", "flat-secret");

        var options = Resolve();

        options.ApiKey.Should().Be("flat-key", "the legacy flat form stays supported (R-1.2)");
        options.ApiSecret.Should().Be("flat-secret");
    }

    [Fact]
    public void AddProjectXApiClient_ShouldPreferTheLegacyFlatVariable_WhenBothConventionsAreSet()
    {
        Environment.SetEnvironmentVariable("PROJECTX_API_KEY", "flat-key");
        Environment.SetEnvironmentVariable("ProjectX__ApiKey", "double-underscore-key");

        var options = Resolve();

        options.ApiKey.Should().Be("flat-key",
            "adding the new convention must not change what an existing deployment resolves to");
    }

    [Fact]
    public void AddProjectXApiClient_ShouldLeaveCredentialsEmpty_WhenNoVariableIsSet()
    {
        var options = Resolve();

        options.ApiKey.Should().BeEmpty();
        options.ApiSecret.Should().BeEmpty();
    }
}
