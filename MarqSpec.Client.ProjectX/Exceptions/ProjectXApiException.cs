namespace MarqSpec.Client.ProjectX.Exceptions;

/// <summary>
/// Exception thrown when an API request fails.
/// </summary>
public class ProjectXApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code of the failed request.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectXApiException"/> class.
    /// </summary>
    public ProjectXApiException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectXApiException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ProjectXApiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectXApiException"/> class with a specified error message and status code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public ProjectXApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectXApiException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ProjectXApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectXApiException"/> class with a specified error message, status code, and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="innerException">The inner exception.</param>
    public ProjectXApiException(string message, int statusCode, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
