namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the side of an order.
/// </summary>
/// <remarks>
/// Wire values are <see cref="Bid"/> = 0 and <see cref="Ask"/> = 1. Zero is a real
/// buy, not "unset" — response models expose <c>OrderSide?</c> so an omitted
/// <c>side</c> is <c>null</c> rather than <see cref="Bid"/> (gh#83).
/// </remarks>
public enum OrderSide
{
    /// <summary>
    /// Bid - buy order. This is the live wire value — do not treat <c>0</c> as unset.
    /// </summary>
    Bid = 0,

    /// <summary>
    /// Ask - sell order.
    /// </summary>
    Ask = 1
}
