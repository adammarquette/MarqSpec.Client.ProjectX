namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the type of order.
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Unknown order type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Limit order - executes at a specific price or better.
    /// </summary>
    Limit = 1,

    /// <summary>
    /// Market order - executes immediately at the best available price.
    /// </summary>
    Market = 2,

    /// <summary>
    /// Stop limit order - becomes a limit order when the stop price is reached.
    /// </summary>
    StopLimit = 3,

    /// <summary>
    /// Stop order - becomes a market order when the stop price is reached.
    /// </summary>
    Stop = 4,

    /// <summary>
    /// Trailing stop order - stop price trails the market by a specified amount.
    /// </summary>
    TrailingStop = 5,

    /// <summary>
    /// Join bid order - automatically adjusts to join the best bid.
    /// </summary>
    JoinBid = 6,

    /// <summary>
    /// Join ask order - automatically adjusts to join the best ask.
    /// </summary>
    JoinAsk = 7
}
