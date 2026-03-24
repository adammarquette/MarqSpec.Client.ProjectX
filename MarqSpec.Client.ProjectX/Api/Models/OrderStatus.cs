namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Unknown status.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Order has been accepted by the system.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Order is pending execution.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Stop order has been triggered.
    /// </summary>
    Triggered = 3,

    /// <summary>
    /// Order has been partially filled.
    /// </summary>
    PartiallyFilled = 4,

    /// <summary>
    /// Order has been completely filled.
    /// </summary>
    Filled = 5,

    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Order has been rejected.
    /// </summary>
    Rejected = 7,

    /// <summary>
    /// Order has expired.
    /// </summary>
    Expired = 8
}
