using FluentAssertions;
using MarqSpec.Client.ProjectX.Configuration;

namespace MarqSpec.Client.ProjectX.Tests.Configuration;

public class ProjectXOptionsTests
{
    [Fact]
    public void Validate_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new ProjectXOptions
        {
            ApiKey = "test-key",
            ApiSecret = "test-secret",
            BaseUrl = "https://api.test.com"
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithMissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new ProjectXOptions
        {
            ApiKey = "",
            ApiSecret = "test-secret",
            BaseUrl = "https://api.test.com"
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key is required*");
    }

    [Fact]
    public void Validate_WithMissingApiSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new ProjectXOptions
        {
            ApiKey = "test-key",
            ApiSecret = "",
            BaseUrl = "https://api.test.com"
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API secret is required*");
    }

    [Fact]
    public void Validate_WithMissingBaseUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new ProjectXOptions
        {
            ApiKey = "test-key",
            ApiSecret = "test-secret",
            BaseUrl = ""
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Base URL is required*");
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new ProjectXOptions();

        // Assert
        options.BaseUrl.Should().Be("https://api.topstepx.com");
        options.WebSocketUserHubUrl.Should().Be("https://rtc.topstepx.com/hubs/user");
        options.WebSocketMarketHubUrl.Should().Be("https://rtc.topstepx.com/hubs/market");
        options.ValidateSslCertificates.Should().BeTrue();
        options.RetryOptions.Should().NotBeNull();
    }

    [Fact]
    public void RetryOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var retryOptions = new RetryOptions();

        // Assert
        retryOptions.MaxRetries.Should().Be(3);
        retryOptions.InitialDelay.Should().Be(TimeSpan.FromSeconds(1));
        retryOptions.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
    }
}
