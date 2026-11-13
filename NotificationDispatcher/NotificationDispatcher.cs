using APNCore;
using FCMCore;
using NotificationCore;

namespace NotificationDispatcher;

/// <summary>
/// The umbrella notification sender. It holds an optional <see cref="IApnClient"/> and
/// <see cref="IFcmClient"/> and routes each <see cref="PushNotification"/> to the right one based on
/// its <see cref="PushNotification.Platform"/>, translating to and from the provider models.
/// </summary>
/// <remarks>
/// <para>
/// Either provider may be absent. A notification whose platform has no configured provider resolves to
/// <see cref="PushResult.NotConfigured"/> rather than throwing, so an app that only ships on one
/// platform can register just that provider and still call this one sender.
/// </para>
/// <para>
/// The instance is thread-safe and designed to be long-lived, like the underlying clients; register it
/// as a singleton.
/// </para>
/// </remarks>
public sealed class NotificationDispatcher : INotificationSender
{
    private readonly IApnClient? _apnClient;
    private readonly IFcmClient? _fcmClient;

    /// <summary>
    /// Creates a dispatcher over the available provider clients. Pass <see langword="null"/> for a
    /// provider your app does not target.
    /// </summary>
    /// <param name="apnClient">The APNs client used for <see cref="DevicePlatform.Apple"/>, or <see langword="null"/>.</param>
    /// <param name="fcmClient">The FCM client used for <see cref="DevicePlatform.Android"/>, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">When neither client is supplied.</exception>
    public NotificationDispatcher(IApnClient? apnClient, IFcmClient? fcmClient)
    {
        if (apnClient is null && fcmClient is null)
            throw new ArgumentException(
                "At least one provider must be configured: supply an IApnClient, an IFcmClient, or both.");

        _apnClient = apnClient;
        _fcmClient = fcmClient;
    }

    /// <summary><see langword="true"/> when an APNs client is configured for Apple devices.</summary>
    public bool SupportsApple => _apnClient is not null;

    /// <summary><see langword="true"/> when an FCM client is configured for Android devices.</summary>
    public bool SupportsAndroid => _fcmClient is not null;

    /// <summary>Returns whether a provider is configured for the given platform.</summary>
    public bool Supports(DevicePlatform platform) => platform switch
    {
        DevicePlatform.Apple => SupportsApple,
        DevicePlatform.Android => SupportsAndroid,
        _ => false,
    };

    /// <inheritdoc />
    public async Task<PushResult> SendAsync(PushNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentException.ThrowIfNullOrWhiteSpace(notification.DeviceToken);

        switch (notification.Platform)
        {
            case DevicePlatform.Apple:
                if (_apnClient is null)
                    return PushResult.NotConfigured(DevicePlatform.Apple);
                var apnResponse = await _apnClient
                    .SendAsync(NotificationMapping.ToApnNotification(notification), cancellationToken)
                    .ConfigureAwait(false);
                return NotificationMapping.FromApnResponse(apnResponse);

            case DevicePlatform.Android:
                if (_fcmClient is null)
                    return PushResult.NotConfigured(DevicePlatform.Android);
                var fcmResponse = await _fcmClient
                    .SendAsync(NotificationMapping.ToFcmMessage(notification), cancellationToken)
                    .ConfigureAwait(false);
                return NotificationMapping.FromFcmResponse(fcmResponse);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(notification), notification.Platform, "Unknown device platform.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PushResult>> SendManyAsync(
        IReadOnlyList<PushNotification> notifications,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        var results = new PushResult[notifications.Count];
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = new Task[notifications.Count];
        for (var i = 0; i < notifications.Count; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    results[index] = await SendAsync(notifications[index], cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    gate.Release();
                }
            }, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }
}
