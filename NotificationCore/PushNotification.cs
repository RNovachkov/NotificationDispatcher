namespace NotificationCore;

/// <summary>
/// A single, provider-agnostic notification: the target device, the user-facing content and an
/// optional data payload. The dispatcher translates this into the APNs or FCM representation based on
/// <see cref="Platform"/>.
/// </summary>
/// <remarks>
/// Only the common fields that both APNs and FCM understand are exposed here. When <see cref="Title"/>
/// and <see cref="Body"/> are both omitted the notification is treated as a silent, data-only
/// delivery (an APNs background push, or an FCM data message) so the receiving app handles it in the
/// background. Prefer the static factory helpers (<see cref="Apple"/>, <see cref="Android"/>,
/// <see cref="Alert"/>) for the common cases.
/// </remarks>
public sealed class PushNotification
{
    /// <summary>The platform of the target device, which selects the provider. Required.</summary>
    public required DevicePlatform Platform { get; init; }

    /// <summary>
    /// The device registration token: the APNs device token for <see cref="DevicePlatform.Apple"/>,
    /// or the FCM registration token for <see cref="DevicePlatform.Android"/>. Required.
    /// </summary>
    public required string DeviceToken { get; init; }

    /// <summary>The notification title shown to the user. Omit (together with <see cref="Body"/>) for a silent push.</summary>
    public string? Title { get; init; }

    /// <summary>The notification body shown to the user. Omit (together with <see cref="Title"/>) for a silent push.</summary>
    public string? Body { get; init; }

    /// <summary>
    /// The URL of an image to display with the notification. On Android this maps to the FCM
    /// notification image; on Apple it is delivered as custom data (<c>image_url</c>) and the push is
    /// marked mutable so a Notification Service Extension can attach it.
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// The number to show on the app icon badge (Apple only). <c>0</c> clears the badge. Ignored for
    /// <see cref="DevicePlatform.Android"/>, where the badge is managed by the app.
    /// </summary>
    public int? Badge { get; init; }

    /// <summary>
    /// The sound to play: a sound file name in the app, or <c>"default"</c> for the system sound.
    /// Applied to the APNs <c>sound</c> field and the FCM Android notification sound.
    /// </summary>
    public string? Sound { get; init; }

    /// <summary>
    /// Arbitrary string key/value data delivered to the app alongside (or instead of) the visible
    /// notification. Both providers require string values.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Data { get; init; }

    /// <summary>
    /// An identifier used to collapse related notifications so only the most recent is shown. Maps to
    /// the APNs <c>apns-collapse-id</c> header and the FCM Android <c>collapse_key</c>.
    /// </summary>
    public string? CollapseId { get; init; }

    /// <summary>
    /// An optional override for the APNs <c>apns-topic</c> (usually the app's bundle ID). Apple only;
    /// when <see langword="null"/> the <c>IApnClient</c>'s configured default topic is used. Ignored
    /// for Android.
    /// </summary>
    public string? ApnsTopic { get; init; }

    /// <summary><see langword="true"/> when neither <see cref="Title"/> nor <see cref="Body"/> is set, i.e. a silent, data-only push.</summary>
    public bool IsSilent => string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Body);

    // -- Factory helpers ----------------------------------------------------------------------

    /// <summary>Creates a user-facing alert for the given platform and device token.</summary>
    public static PushNotification Alert(DevicePlatform platform, string deviceToken, string title, string body) =>
        new() { Platform = platform, DeviceToken = deviceToken, Title = title, Body = body };

    /// <summary>Creates a user-facing alert for an Apple device.</summary>
    public static PushNotification Apple(string deviceToken, string title, string body) =>
        Alert(DevicePlatform.Apple, deviceToken, title, body);

    /// <summary>Creates a user-facing alert for an Android device.</summary>
    public static PushNotification Android(string deviceToken, string title, string body) =>
        Alert(DevicePlatform.Android, deviceToken, title, body);

    /// <summary>Creates a silent, data-only push for the given platform (APNs background / FCM data message).</summary>
    public static PushNotification DataOnly(DevicePlatform platform, string deviceToken, IReadOnlyDictionary<string, string> data) =>
        new() { Platform = platform, DeviceToken = deviceToken, Data = data };
}
