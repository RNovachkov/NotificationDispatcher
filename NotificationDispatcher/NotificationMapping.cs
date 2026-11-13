using APNCore;
using APNCore.Models;
using FCMCore;
using FCMCore.Models;
using NotificationCore;

namespace NotificationDispatcher;

/// <summary>
/// Translates the provider-agnostic <see cref="PushNotification"/> and <see cref="PushResult"/> types
/// to and from the APNs and FCM representations. Kept internal: the public surface is
/// <see cref="INotificationSender"/> and its models only.
/// </summary>
internal static class NotificationMapping
{
    /// <summary>The custom-data key under which an Apple notification carries its image URL.</summary>
    internal const string AppleImageKey = "image_url";

    // -- Apple (APNs) -------------------------------------------------------------------------

    /// <summary>Builds the APNs notification for a unified push. Silent pushes become a background notification.</summary>
    internal static ApnNotification ToApnNotification(PushNotification n)
    {
        var silent = n.IsSilent;

        var aps = new Aps
        {
            Badge = n.Badge,
            Sound = n.Sound,
        };

        if (silent)
        {
            // No visible content: deliver as a background push so the app wakes to handle the data.
            aps.ContentAvailable = 1;
        }
        else
        {
            aps.Alert = new ApnAlert { Title = n.Title, Body = n.Body };

            // An image can only be attached on-device by a Notification Service Extension, which runs
            // only for a mutable alert.
            if (!string.IsNullOrEmpty(n.ImageUrl))
                aps.MutableContent = 1;
        }

        var notification = new ApnNotification(n.DeviceToken, silent ? ApnPushType.Background : ApnPushType.Alert)
        {
            Topic = n.ApnsTopic,
            Priority = silent ? ApnPriority.Medium : ApnPriority.High,
            CollapseId = n.CollapseId,
            Payload = new ApnPayload { Aps = aps },
        };

        var custom = BuildAppleCustomData(n);
        if (custom is not null)
            notification.Payload.CustomData = custom;

        return notification;
    }

    private static Dictionary<string, object?>? BuildAppleCustomData(PushNotification n)
    {
        var hasData = n.Data is { Count: > 0 };
        var hasImage = !string.IsNullOrEmpty(n.ImageUrl);
        if (!hasData && !hasImage)
            return null;

        var custom = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (n.Data is not null)
        {
            foreach (var (key, value) in n.Data)
                custom[key] = value;
        }

        if (hasImage)
            custom[AppleImageKey] = n.ImageUrl;

        return custom;
    }

    // -- Android (FCM) ------------------------------------------------------------------------

    /// <summary>Builds the FCM message for a unified push. Silent pushes become a data-only message.</summary>
    internal static FcmMessage ToFcmMessage(PushNotification n)
    {
        var message = new FcmMessage
        {
            Token = n.DeviceToken,
            Data = n.Data is { Count: > 0 } ? new Dictionary<string, string>(n.Data) : null,
        };

        if (!n.IsSilent)
        {
            message.Notification = new FcmNotification
            {
                Title = n.Title,
                Body = n.Body,
                Image = string.IsNullOrEmpty(n.ImageUrl) ? null : n.ImageUrl,
            };
        }

        var android = BuildAndroidConfig(n);
        if (android is not null)
            message.Android = android;

        return message;
    }

    private static AndroidConfig? BuildAndroidConfig(PushNotification n)
    {
        var hasCollapse = !string.IsNullOrEmpty(n.CollapseId);
        var hasSound = !string.IsNullOrEmpty(n.Sound);

        // A silent/data-only message is delivered at high priority so the app's background handler is
        // woken reliably, matching the FcmMessage.DataMessage factory.
        if (!hasCollapse && !hasSound && !n.IsSilent)
            return null;

        return new AndroidConfig
        {
            CollapseKey = hasCollapse ? n.CollapseId : null,
            Priority = n.IsSilent ? AndroidMessagePriority.High : null,
            Notification = hasSound ? new AndroidNotification { Sound = n.Sound } : null,
        };
    }

    // -- Responses ----------------------------------------------------------------------------

    /// <summary>Maps an APNs response onto the unified result.</summary>
    internal static PushResult FromApnResponse(ApnResponse response) => new()
    {
        IsSuccess = response.IsSuccess,
        Platform = DevicePlatform.Apple,
        StatusCode = response.StatusCode,
        MessageId = response.ApnsId,
        ErrorCode = response.Reason,
        ErrorMessage = response.Reason,
        ShouldRemoveToken = response.ShouldRemoveToken,
        ProviderResponse = response,
    };

    /// <summary>Maps an FCM response onto the unified result.</summary>
    internal static PushResult FromFcmResponse(FcmResponse response) => new()
    {
        IsSuccess = response.IsSuccess,
        Platform = DevicePlatform.Android,
        StatusCode = response.StatusCode,
        MessageId = response.MessageName,
        ErrorCode = response.ErrorCode ?? response.ErrorStatus,
        ErrorMessage = response.Reason,
        ShouldRemoveToken = response.ShouldRemoveToken,
        ProviderResponse = response,
    };
}
