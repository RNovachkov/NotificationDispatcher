namespace NotificationCore;

/// <summary>
/// The kind of device a notification targets. The dispatcher uses this to pick the underlying
/// provider: <see cref="Apple"/> notifications go through Apple Push Notification service (APNs) and
/// <see cref="Android"/> notifications go through Firebase Cloud Messaging (FCM).
/// </summary>
public enum DevicePlatform
{
    /// <summary>
    /// An Apple device (iPhone, iPad, Mac, Apple Watch or Apple TV). Delivered through APNs using the
    /// registered <c>IApnClient</c>.
    /// </summary>
    Apple,

    /// <summary>
    /// An Android device. Delivered through FCM using the registered <c>IFcmClient</c>.
    /// </summary>
    Android,
}
