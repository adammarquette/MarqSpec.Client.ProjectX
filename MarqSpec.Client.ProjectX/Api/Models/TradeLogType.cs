namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Direction of a market trade log entry, matching the ProjectX <c>TradeLogType</c> wire enum.
/// </summary>
/// <remarks>
/// The live hub documents <c>type: 0</c> as Buy and <c>type: 1</c> as Sell
/// (<see href="https://gateway.docs.projectx.com/docs/realtime/"/>). Those
/// integers are load-bearing: renumbering this enum so zero means "unknown"
/// would rewrite a stated Buy as absence. Absence itself is represented by
/// a null <see cref="TradeUpdate.Type"/> (gh#86, R-5.8).
/// </remarks>
public enum TradeLogType
{
    /// <summary>Buy-side trade. Wire value <c>0</c>.</summary>
    Buy = 0,

    /// <summary>Sell-side trade. Wire value <c>1</c>.</summary>
    Sell = 1,
}
