using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;

namespace MarqSpec.Client.ProjectX.Tests.Resilience;

public class RetryAfterTests
{
    /// <summary>
    /// Mirrors the DelayGenerator logic from ServiceCollectionExtensions to test it in isolation.
    /// </summary>
    private static TimeSpan? ComputeRetryDelay(HttpResponseMessage? response)
    {
        if (response is { StatusCode: HttpStatusCode.TooManyRequests }
            && response.Headers.RetryAfter is { } retryAfter)
        {
            TimeSpan? delay = retryAfter.Delta
                ?? (retryAfter.Date.HasValue
                    ? retryAfter.Date.Value - DateTimeOffset.UtcNow
                    : null);

            if (delay.HasValue && delay.Value > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return null; // fall back to default exponential backoff
    }

    [Fact]
    public void DelayGenerator_With429AndRetryAfterDelta_ReturnsSpecifiedDelay()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));

        // Act
        var delay = ComputeRetryDelay(response);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DelayGenerator_With429AndRetryAfterDate_ReturnsComputedDelay()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var futureDate = DateTimeOffset.UtcNow.AddSeconds(10);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(futureDate);

        // Act
        var delay = ComputeRetryDelay(response);

        // Assert
        delay.Should().NotBeNull();
        delay!.Value.Should().BeCloseTo(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DelayGenerator_With429AndRetryAfterDateInPast_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var pastDate = DateTimeOffset.UtcNow.AddSeconds(-5);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(pastDate);

        // Act
        var delay = ComputeRetryDelay(response);

        // Assert
        delay.Should().BeNull("past dates should fall back to default backoff");
    }

    [Fact]
    public void DelayGenerator_With429AndNoRetryAfterHeader_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        // Act
        var delay = ComputeRetryDelay(response);

        // Assert
        delay.Should().BeNull("missing Retry-After should fall back to default backoff");
    }

    [Fact]
    public void DelayGenerator_With500Error_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        // Act
        var delay = ComputeRetryDelay(response);

        // Assert
        delay.Should().BeNull("non-429 errors should fall back to default backoff");
    }

    [Fact]
    public void DelayGenerator_WithNullResponse_ReturnsNull()
    {
        // Act
        var delay = ComputeRetryDelay(null);

        // Assert
        delay.Should().BeNull();
    }

    [Fact]
    public void DelayGenerator_With429AndZeroDelta_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

        // Act
        var delay = ComputeRetryDelay(response);

        // Assert
        delay.Should().BeNull("zero delay should fall back to default backoff");
    }
}
