using System.Text.Json;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;

namespace MarqSpec.Client.ProjectX.Tests.MarketData;

/// <summary>
/// Pins the <c>GatewayTrade</c> payload's <c>type</c> field (gh#86, R-5.8).
/// </summary>
/// <remarks>
/// The live wire is documented at
/// <see href="https://gateway.docs.projectx.com/docs/realtime/"/>:
/// <c>type: 0</c> is Buy and <c>type: 1</c> is Sell. A non-nullable
/// <see cref="TradeLogType"/> therefore deserialises an omitted or null
/// <c>type</c> as <see cref="TradeLogType.Buy"/> — every unstated print
/// becomes buying pressure. These tests use the same
/// <see cref="JsonSerializerDefaults.Web"/> settings the hubs speak.
/// </remarks>
public class TradeUpdateDeserializationTests
{
    private static readonly JsonSerializerOptions _web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deserialize_ShouldNotArriveAsBuy_WhenTypeIsOmitted()
    {
        const string json =
            """{"symbolId":"F.US.EP","price":2100.25,"timestamp":"2024-07-21T13:45:00Z","volume":2}""";

        var update = JsonSerializer.Deserialize<TradeUpdate>(json, _web);

        update.Should().NotBeNull();
        update!.Type.Should().NotBe(TradeLogType.Buy,
            "an omitted type is the venue saying nothing, not a buy (gh#86)");
        update.Type.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ShouldNotArriveAsBuy_WhenTypeIsNull()
    {
        const string json =
            """{"symbolId":"F.US.EP","price":2100.25,"timestamp":"2024-07-21T13:45:00Z","type":null,"volume":2}""";

        var update = JsonSerializer.Deserialize<TradeUpdate>(json, _web);

        update.Should().NotBeNull();
        update!.Type.Should().NotBe(TradeLogType.Buy);
        update.Type.Should().BeNull();
    }

    [Theory]
    [InlineData(0, TradeLogType.Buy)]
    [InlineData(1, TradeLogType.Sell)]
    public void Deserialize_ShouldMapTheDocumentedWireValues(int wire, TradeLogType expected)
    {
        var json =
            $$"""{"symbolId":"F.US.EP","price":2100.25,"timestamp":"2024-07-21T13:45:00Z","type":{{wire}},"volume":2}""";

        var update = JsonSerializer.Deserialize<TradeUpdate>(json, _web);

        update.Should().NotBeNull();
        update!.Type.Should().Be(expected,
            "ProjectX documents type 0 = Buy and type 1 = Sell; those values must survive");
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(2)]
    public void Deserialize_ShouldNotArriveAsBuy_WhenTypeIsUnrecognised(int wire)
    {
        var json =
            $$"""{"symbolId":"F.US.EP","price":2100.25,"timestamp":"2024-07-21T13:45:00Z","type":{{wire}},"volume":2}""";

        var update = JsonSerializer.Deserialize<TradeUpdate>(json, _web);

        update.Should().NotBeNull();
        update!.Type.Should().NotBe(TradeLogType.Buy);
        update.Type.Should().BeNull("an unrecognised direction is not a stated Buy or Sell");
    }

    [Fact]
    public void Deserialize_ShouldNotArriveAsBuy_WhenTypeIsUnparseable()
    {
        const string json =
            """{"symbolId":"F.US.EP","price":2100.25,"timestamp":"2024-07-21T13:45:00Z","type":"nope","volume":2}""";

        var update = JsonSerializer.Deserialize<TradeUpdate>(json, _web);

        update.Should().NotBeNull();
        update!.Type.Should().NotBe(TradeLogType.Buy);
        update.Type.Should().BeNull();
    }

    [Fact]
    public void Serialize_ShouldEmitWireZero_WhenTypeIsBuy()
    {
        var update = new TradeUpdate { Type = TradeLogType.Buy };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(update, _web));

        document.RootElement.GetProperty("type").GetInt32().Should().Be(0,
            "round-tripping must keep the documented wire value so a consumer does not emit Sell as Buy");
    }
}
