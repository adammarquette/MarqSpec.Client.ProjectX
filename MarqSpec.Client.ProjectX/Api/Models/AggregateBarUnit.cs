namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the time unit for aggregating bars.
/// </summary>
public enum AggregateBarUnit
{
    /// <summary>
    /// Unspecified unit.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Second-based aggregation.
    /// </summary>
    Second = 1,

    /// <summary>
    /// Minute-based aggregation.
    /// </summary>
    Minute = 2,

    /// <summary>
    /// Hour-based aggregation.
    /// </summary>
    Hour = 3,

    /// <summary>
    /// Day-based aggregation.
    /// </summary>
    Day = 4,

    /// <summary>
    /// Week-based aggregation.
    /// </summary>
    Week = 5,

    /// <summary>
    /// Month-based aggregation.
    /// </summary>
    Month = 6
}
