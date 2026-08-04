using System.Text.Json;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;

namespace MarqSpec.Client.ProjectX.Tests.OrderManagement;

/// <summary>
/// Guards the <b>wire contract</b> of <see cref="SearchOrderRequest"/> (gh#642). The bug this pins: the window
/// properties were named <c>startTime</c> / <c>endTime</c>, but the gateway's <c>/api/Order/search</c> schema
/// requires <c>startTimestamp</c> / <c>endTimestamp</c> — so the window was silently dropped and the gh#631
/// fill-veto's order-history read returned nothing while looking shipped. The existing client tests asserted the
/// C# <i>property</i>, never the serialized name, which is exactly why this slipped through — so this test checks
/// the emitted JSON keys directly.
/// </summary>
public class SearchOrderRequestSerializationTests
{
    [Fact]
    public void Serialize_EmitsTheGatewaysTimestampFieldNames_NotTime()
    {
        var request = new SearchOrderRequest
        {
            AccountId = 42,
            StartTimestamp = new DateTime(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc),
            EndTimestamp = new DateTime(2026, 8, 4, 20, 0, 0, DateTimeKind.Utc),
        };

        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(request));
        JsonElement root = document.RootElement;

        root.TryGetProperty("startTimestamp", out _).Should().BeTrue("the gateway requires startTimestamp");
        root.TryGetProperty("endTimestamp", out _).Should().BeTrue("the gateway names the range end endTimestamp");
        root.TryGetProperty("startTime", out _).Should().BeFalse("startTime is the gh#642 bug — the gateway ignores it");
        root.TryGetProperty("endTime", out _).Should().BeFalse("endTime is the gh#642 bug — the gateway ignores it");
    }
}
