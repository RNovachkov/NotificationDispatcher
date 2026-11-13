using APNCore;
using FCMCore;

namespace NotificationDispatcher;

/// <summary>
/// Collects the provider configuration for a single <c>AddNotifications</c> call. Configure the
/// platforms your app targets with <see cref="UseApns"/> and/or <see cref="UseFcm"/>; whichever you
/// set is registered, and the dispatcher routes to it automatically.
/// </summary>
public sealed class NotificationBuilder
{
    internal Action<ApnsSettings>? ApnsConfigure { get; private set; }

    internal Action<FcmSettings>? FcmConfigure { get; private set; }

    /// <summary>
    /// Configures Apple Push Notification service (APNs) for <see cref="NotificationCore.DevicePlatform.Apple"/>
    /// devices. The delegate populates the same <see cref="ApnsSettings"/> that <c>AddApns</c> uses.
    /// </summary>
    /// <param name="configure">Populates the APNs settings (team ID, key ID, <c>.p8</c> path, default topic, …).</param>
    /// <returns>The same builder, for chaining.</returns>
    public NotificationBuilder UseApns(Action<ApnsSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ApnsConfigure = configure;
        return this;
    }

    /// <summary>
    /// Configures Firebase Cloud Messaging (FCM) for <see cref="NotificationCore.DevicePlatform.Android"/>
    /// devices. The delegate populates the same <see cref="FcmSettings"/> that <c>AddFcm</c> uses.
    /// </summary>
    /// <param name="configure">Populates the FCM settings (service-account file path, project ID, …).</param>
    /// <returns>The same builder, for chaining.</returns>
    public NotificationBuilder UseFcm(Action<FcmSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        FcmConfigure = configure;
        return this;
    }
}
