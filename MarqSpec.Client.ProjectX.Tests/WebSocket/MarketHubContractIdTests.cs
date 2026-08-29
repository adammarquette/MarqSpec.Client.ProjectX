using FakeItEasy;
using FluentAssertions;
using MarqSpec.Client.ProjectX.Api.Models;
using MarqSpec.Client.ProjectX.Authentication;
using MarqSpec.Client.ProjectX.Configuration;
using MarqSpec.Client.ProjectX.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarqSpec.Client.ProjectX.Tests.WebSocket;

/// <summary>
/// Pins the market-hub <c>contractId</c> argument onto the raised update (gh#86, R-5.7).
/// </summary>
/// <remarks>
/// <c>GatewayTrade</c>, <c>GatewayQuote</c> and <c>GatewayDepth</c> all arrive as
/// <c>(contractId, payload)</c>. The payload's symbol is a product root
/// (<c>F.US.EP</c>), and depth has no symbol at all. Dropping the hub argument
/// makes two expiries of one root indistinguishable. These tests drive the
/// bind handlers directly — no socket — so a regression is a failed assertion,
/// not a silent merge of two tapes.
/// </remarks>
public class MarketHubContractIdTests
{
    private const string Front = "CON.F.US.EP.Z26";
    private const string Back = "CON.F.US.EP.H27";
    private const string Root = "F.US.EP";

    [Fact]
    public void HandleGatewayTrade_ShouldAttributeEachPrintToTheHubContract_WhenTwoExpiriesShareARoot()
    {
        var client = CreateClient();
        var received = new List<TradeUpdate>();
        client.TradeUpdateReceived += (_, update) => received.Add(update);

        client.HandleGatewayTrade(Front, [new TradeUpdate { SymbolId = Root, Price = 2100.25m, Volume = 2 }]);
        client.HandleGatewayTrade(Back, [new TradeUpdate { SymbolId = Root, Price = 2101.00m, Volume = 1 }]);

        received.Should().HaveCount(2);
        received[0].ContractId.Should().Be(Front);
        received[1].ContractId.Should().Be(Back);
        received[0].SymbolId.Should().Be(Root, "the payload symbol stays the product root");
        received[1].SymbolId.Should().Be(Root);
    }

    [Fact]
    public void HandleGatewayQuote_ShouldStampHubContractId_WhenPayloadSymbolIsTheProductRoot()
    {
        var client = CreateClient();
        PriceUpdate? received = null;
        client.PriceUpdateReceived += (_, update) => received = update;

        client.HandleGatewayQuote(Front, new PriceUpdate { Symbol = Root, LastPrice = 2100.25m });

        received.Should().NotBeNull();
        received!.ContractId.Should().Be(Front);
        received.Symbol.Should().Be(Root);
    }

    [Fact]
    public void HandleGatewayDepth_ShouldStampHubContractId_WhenThePayloadHasNoSymbol()
    {
        var client = CreateClient();
        OrderBookUpdate? received = null;
        client.OrderBookUpdateReceived += (_, update) => received = update;

        client.HandleGatewayDepth(Back, [new OrderBookUpdate { Price = 2100.00m, Volume = 10, Type = DomType.Ask }]);

        received.Should().NotBeNull();
        received!.ContractId.Should().Be(Back);
    }

    [Fact]
    public void HandleGatewayTrade_ShouldRaiseNothing_WhenThePayloadArrayIsNull()
    {
        var client = CreateClient();
        var raised = false;
        client.TradeUpdateReceived += (_, _) => raised = true;

        client.HandleGatewayTrade(Front, null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void HandleGatewayQuote_ShouldRaiseNothing_WhenTheUpdateIsNull()
    {
        var client = CreateClient();
        var raised = false;
        client.PriceUpdateReceived += (_, _) => raised = true;

        client.HandleGatewayQuote(Front, null);

        raised.Should().BeFalse();
    }

    private static ProjectXWebSocketClient CreateClient()
    {
        return new ProjectXWebSocketClient(
            A.Fake<IAuthenticationService>(),
            Options.Create(new WebSocketOptions()),
            A.Fake<ILoggerFactory>(),
            A.Fake<ILogger<ProjectXWebSocketClient>>());
    }
}
