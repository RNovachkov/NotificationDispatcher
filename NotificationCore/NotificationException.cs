namespace NotificationCore;

/// <summary>
/// Thrown when a provider rejects a notification and the caller requested that failures throw (for
/// example via <see cref="PushResult.EnsureSuccess"/>).
/// </summary>
public sealed class NotificationException : Exception
{
    /// <summary>Creates the exception from a failed <see cref="PushResult"/>.</summary>
    public NotificationException(PushResult result)
        : base($"Sending the {result.Platform} notification failed" +
               (result.StatusCode is { } status ? $" with status {status}" : "") +
               (result.ErrorCode is { } code ? $" ({code})." : ".") +
               (result.ErrorMessage is { } message ? $" {message}" : ""))
    {
        Result = result;
    }

    /// <summary>The failed result returned by the dispatcher.</summary>
    public PushResult Result { get; }
}
