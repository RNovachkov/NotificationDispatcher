namespace NotificationCore;

/// <summary>
/// The provider-agnostic result of attempting to send a <see cref="PushNotification"/>. It flattens
/// the APNs and FCM responses into one shape so callers do not need to branch on the platform.
/// </summary>
public sealed record PushResult
{
    /// <summary><see langword="true"/> when the provider accepted the notification.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>The platform the notification was sent to.</summary>
    public required DevicePlatform Platform { get; init; }

    /// <summary>
    /// The HTTP status code returned by the provider, or <see langword="null"/> when the notification
    /// never reached a provider (for example <see cref="NotConfigured"/>).
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// On success, the provider's identifier for the message: the APNs <c>apns-id</c>, or the FCM
    /// message name (<c>projects/{id}/messages/{id}</c>). <see langword="null"/> on failure.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// On failure, the provider's machine-readable error code: the APNs <c>reason</c> (for example
    /// <c>BadDeviceToken</c>), or the FCM error code / status (for example <c>UNREGISTERED</c>).
    /// <see langword="null"/> on success.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>On failure, a human-readable description of what went wrong. <see langword="null"/> on success.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// <see langword="true"/> when the provider reported that the device token is permanently invalid
    /// and should be removed from your database (unrolls the provider's own <c>ShouldRemoveToken</c>).
    /// </summary>
    public bool ShouldRemoveToken { get; init; }

    /// <summary>
    /// The underlying provider response (<c>ApnResponse</c> or <c>FcmResponse</c>) when one was
    /// produced, for callers that need provider-specific detail. <see langword="null"/> when no
    /// provider was invoked.
    /// </summary>
    public object? ProviderResponse { get; init; }

    /// <summary>
    /// Creates the result returned when a notification targets a platform whose provider is not
    /// registered (for example an Android push when only APNs was configured). It is not a success and
    /// carries no status code.
    /// </summary>
    /// <param name="platform">The platform that had no configured provider.</param>
    public static PushResult NotConfigured(DevicePlatform platform) => new()
    {
        IsSuccess = false,
        Platform = platform,
        ErrorCode = "ProviderNotConfigured",
        ErrorMessage = $"No notification provider is registered for {platform}. " +
            (platform == DevicePlatform.Apple
                ? "Register an IApnClient (for example via AddApns or AddNotifications(...UseApns...))."
                : "Register an IFcmClient (for example via AddFcm or AddNotifications(...UseFcm...))."),
    };

    /// <summary>Throws a <see cref="NotificationException"/> when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public PushResult EnsureSuccess()
    {
        if (!IsSuccess)
            throw new NotificationException(this);
        return this;
    }
}
