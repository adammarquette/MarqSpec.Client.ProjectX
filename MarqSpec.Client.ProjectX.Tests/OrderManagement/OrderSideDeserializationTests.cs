using System.Text.Json;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;

namespace MarqSpec.Client.ProjectX.Tests.OrderManagement;

/// <summary>
/// Pins <c>side</c> on the four response models that bind <see cref="OrderSide"/> (gh#83, R-3.2).
/// </summary>
/// <remarks>
/// <see cref="OrderSide"/> is <c>Bid = 0</c>, <c>Ask = 1</c>. A non-nullable
/// <c>Side</c> therefore deserialises an omitted field as <see cref="OrderSide.Bid"/>,
/// byte-identical to an explicit <c>"side": 0</c>. Consumers then report a confident
/// buy for an order the venue never gave a direction to. These tests use the same
/// <see cref="JsonSerializerDefaults.Web"/> settings the REST layer uses, with no
/// string-enum converter (ADR-0001).
/// </remarks>
public class OrderSideDeserializationTests
{
    private static readonly JsonSerializerOptions _web = new(JsonSerializerDefaults.Web);

    public static TheoryData<Type> ResponseModels { get; } =
    [
        typeof(Order),
        typeof(HalfTrade),
        typeof(OrderUpdate),
        typeof(TradeNotification),
    ];

    [Theory]
    [MemberData(nameof(ResponseModels))]
    public void Deserialize_ShouldBindSideToNull_WhenSideFieldIsOmitted(Type modelType)
    {
        const string json = """{"id":1,"contractId":"C","size":2}""";

        var model = JsonSerializer.Deserialize(json, modelType, _web);

        model.Should().NotBeNull();
        ReadSide(model!).Should().BeNull(
            "an omitted side is the venue saying nothing, not a bid (gh#83)");
    }

    [Theory]
    [MemberData(nameof(ResponseModels))]
    public void Deserialize_ShouldBindSideToBid_WhenSideIsZero(Type modelType)
    {
        const string json = """{"id":1,"contractId":"C","size":2,"side":0}""";

        var model = JsonSerializer.Deserialize(json, modelType, _web);

        model.Should().NotBeNull();
        ReadSide(model!).Should().Be(OrderSide.Bid);
    }

    [Theory]
    [MemberData(nameof(ResponseModels))]
    public void Deserialize_ShouldBindSideToAsk_WhenSideIsOne(Type modelType)
    {
        const string json = """{"id":1,"contractId":"C","size":2,"side":1}""";

        var model = JsonSerializer.Deserialize(json, modelType, _web);

        model.Should().NotBeNull();
        ReadSide(model!).Should().Be(OrderSide.Ask);
    }

    [Theory]
    [MemberData(nameof(ResponseModels))]
    public void Deserialize_ShouldPreserveOutOfRangeValue_WhenSideIsNine(Type modelType)
    {
        const string json = """{"id":1,"contractId":"C","size":2,"side":9}""";

        var model = JsonSerializer.Deserialize(json, modelType, _web);

        model.Should().NotBeNull();
        ReadSide(model!).Should().Be((OrderSide)9);
    }

    [Theory]
    [MemberData(nameof(ResponseModels))]
    public void Deserialize_ShouldThrowJsonException_WhenSideIsNull(Type modelType)
    {
        const string json = """{"id":1,"contractId":"C","size":2,"side":null}""";

        var act = () => JsonSerializer.Deserialize(json, modelType, _web);

        act.Should().Throw<JsonException>(
            "swagger types side as OrderSide, not nullable-and-present");
    }

    [Fact]
    public void Deserialize_ShouldKeepPlaceOrderRequestSideRequired_WhenSideIsOmitted()
    {
        const string json = """{"accountId":1,"contractId":"C","type":2,"size":2}""";

        var request = JsonSerializer.Deserialize<PlaceOrderRequest>(json, _web);

        request.Should().NotBeNull();
        request!.Side.Should().Be(OrderSide.Bid,
            "PlaceOrderRequest.Side stays a required outbound field; do not null it (gh#83)");
    }

    private static OrderSide? ReadSide(object model) => model switch
    {
        Order order => order.Side,
        HalfTrade trade => trade.Side,
        OrderUpdate update => update.Side,
        TradeNotification notification => notification.Side,
        _ => throw new InvalidOperationException($"Unexpected model {model.GetType().Name}."),
    };
}
