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
        options.RetryOptions.Should().NotBeNull();
    }

    // gh#69 -- ValidateSslCertificates is bound and documented but deliberately not honoured. Asserting the
    // attribute rather than the default value is the point: the contract being tested is "this does nothing,
    // on purpose", and a plain default-value assertion would keep passing if someone quietly wired it up.
    [Fact]
    public void ValidateSslCertificates_ShouldBeObsolete_BecauseTlsValidationCannotBeDisabled()
    {
#pragma warning disable CS0618 // nameof still trips the obsolete diagnostic; the attribute is the subject here
        var property = typeof(ProjectXOptions).GetProperty(nameof(ProjectXOptions.ValidateSslCertificates));
#pragma warning restore CS0618

        var obsolete = property!.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
            .Cast<ObsoleteAttribute>()
            .SingleOrDefault();

        obsolete.Should().NotBeNull();
        obsolete!.IsError.Should().BeFalse("removing it would break consumers; it is a warning, not an error");
        obsolete.Message.Should().Contain("no effect");
    }

    [Fact]
    public void UseMessagePack_ShouldBeObsolete_BecauseTheHubsAlwaysSpeakJson()
    {
#pragma warning disable CS0618 // nameof still trips the obsolete diagnostic; the attribute is the subject here
        var property = typeof(WebSocketOptions).GetProperty(nameof(WebSocketOptions.UseMessagePack));
#pragma warning restore CS0618

        var obsolete = property!.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
            .Cast<ObsoleteAttribute>()
            .SingleOrDefault();

        obsolete.Should().NotBeNull();
        obsolete!.IsError.Should().BeFalse();
        obsolete.Message.Should().Contain("no effect");
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
