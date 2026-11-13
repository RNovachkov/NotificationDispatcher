namespace NotificationCore;

/// <summary>
/// Sends provider-agnostic <see cref="PushNotification"/>s, routing each to Apple Push Notification
/// service (APNs) or Firebase Cloud Messaging (FCM) based on its <see cref="PushNotification.Platform"/>.
/// This is the single entry point consumers depend on; the concrete dispatcher picks whichever
/// provider(s) are configured.
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Sends a single notification and returns the result. This does not throw for provider-level
    /// rejections (invalid token, unconfigured provider, etc.); inspect <see cref="PushResult.IsSuccess"/>
    /// or call <see cref="PushResult.EnsureSuccess"/>.
    /// </summary>
    /// <param name="notification">The notification to deliver.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    Task<PushResult> SendAsync(PushNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends many notifications concurrently (across both providers as needed), returning a result for
    /// each in the same order as the input. Failures are reported per-notification rather than throwing.
    /// </summary>
    /// <param name="notifications">The notifications to deliver.</param>
    /// <param name="maxConcurrency">The maximum number of in-flight requests. Defaults to 10.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<PushResult>> SendManyAsync(
        IReadOnlyList<PushNotification> notifications,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default);
}
