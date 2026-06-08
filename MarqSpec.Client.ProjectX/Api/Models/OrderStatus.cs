namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>No status.</summary>
    None = 0,

    /// <summary>Order is open and working.</summary>
    Open = 1,

    /// <summary>Order has been completely filled.</summary>
    Filled = 2,

    /// <summary>Order has been cancelled.</summary>
    Cancelled = 3,

    /// <summary>Order has expired.</summary>
    Expired = 4,

    /// <summary>Order has been rejected.</summary>
    Rejected = 5,

    /// <summary>Order is pending submission.</summary>
    Pending = 6,
}

