namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Represents the direction of an open position.
/// </summary>
public enum PositionType
{
    /// <summary>
    /// Position direction is undefined.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// A long (buy) position.
    /// </summary>
    Long = 1,

    /// <summary>
    /// A short (sell) position.
    /// </summary>
    Short = 2
}
