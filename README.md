![logo](<NotificationDispatcher/Assets/NotificationDispatcher.png>)

# Notification Dispatcher

# Unified Notifications

One umbrella over the `APNService` (Apple / APNs) and `FCMService`
(Android / FCM) NuGet packages. Describe a notification with a handful of
common parameters plus a **device platform**, and the dispatcher picks the right underlying provider
— as long as that provider is configured.

## Installation

```bash
dotnet add package NotificationDispatcher
```

Targets `net10.0`. The `APNService` and `FCMService` providers come in as package
dependencies, so you only need this one package.

Two projects, mirroring the `*Core` + `*Service` split the provider libraries already use:

| Project | Role |
| --- | --- |
| **`NotificationCore`** | The abstract parameters: `DevicePlatform`, `PushNotification`, `PushResult`, `INotificationSender`. No reference to APNs or FCM. |
| **`NotificationDispatcher`** | The combining logic: maps the unified model to `ApnNotification` / `FcmMessage`, routes by platform to the registered `IApnClient` / `IFcmClient`, maps the response back, and provides the `AddNotifications` DI helpers. |

## The contract

```csharp
public enum DevicePlatform { Apple, Android }

public interface INotificationSender
{
    Task<PushResult> SendAsync(PushNotification notification, CancellationToken ct = default);
    Task<IReadOnlyList<PushResult>> SendManyAsync(
        IReadOnlyList<PushNotification> notifications, int maxConcurrency = 10, CancellationToken ct = default);
}
```

A `PushNotification` carries only what both providers understand: `Platform`, `DeviceToken`,
`Title`, `Body`, `ImageUrl`, `Badge`, `Sound`, `Data`, `CollapseId` (and an optional `ApnsTopic`
override for Apple). A `PushResult` flattens both provider responses into one shape — `IsSuccess`,
`StatusCode`, `MessageId`, `ErrorCode`, `ErrorMessage`, `ShouldRemoveToken` — so callers never branch
on the platform.

## Registering it

Configure both platforms in one call. Whichever you configure is wired up; the dispatcher routes to it.

```csharp
services.AddNotifications(n => n
    .UseApns(a =>
    {
        a.TeamId       = "ABCDE12345";
        a.KeyId        = "KEY1234567";
        a.P8FilePath   = "AuthKey_KEY1234567.p8";
        a.DefaultTopic = "com.example.app";   // the apns-topic used when a notification sets none
    })
    .UseFcm(f =>
    {
        f.ServiceAccountFilePath = "service-account.json";
    }));
```

Only ship on one platform? Configure only that one — the other simply stays unregistered:

```csharp
services.AddNotifications(n => n.UseFcm(f => f.ServiceAccountFilePath = "service-account.json"));
```

Already registering the providers yourself with `AddApns(...)` / `AddFcm(...)`? Use the parameterless
overload; it picks up whatever `IApnClient` / `IFcmClient` are in the container:

```csharp
services
    .AddApns(a => { /* ... */ })
    .AddFcm(f => { /* ... */ })
    .AddNotifications();
```

## Sending

```csharp
public sealed class Notifier(INotificationSender sender)
{
    public async Task PingAsync(string token, DevicePlatform platform)
    {
        var result = await sender.SendAsync(new PushNotification
        {
            Platform    = platform,
            DeviceToken = token,
            Title       = "Hello",
            Body        = "It works on both platforms.",
            Badge       = 1,
            Sound       = "default",
            Data        = new Dictionary<string, string> { ["screen"] = "home" },
        });

        if (!result.IsSuccess)
        {
            if (result.ShouldRemoveToken)
                RemoveFromDatabase(token);           // app uninstalled / token expired on either provider
            else
                Log(result.ErrorCode, result.ErrorMessage);
        }
    }
}
```

Factory helpers cover the common cases: `PushNotification.Apple(token, title, body)`,
`PushNotification.Android(token, title, body)`, `PushNotification.Alert(platform, token, title, body)`
and `PushNotification.DataOnly(platform, token, data)`.

## How the mapping works

| Unified field | Apple (APNs) | Android (FCM) |
| --- | --- | --- |
| `Title` / `Body` | `aps.alert.title` / `.body` | `notification.title` / `.body` |
| `ImageUrl` | custom `image_url` + `mutable-content: 1`¹ | `notification.image` |
| `Badge` | `aps.badge` | — (app-managed) |
| `Sound` | `aps.sound` | `android.notification.sound` |
| `Data` | top-level custom keys | `data` |
| `CollapseId` | `apns-collapse-id` header | `android.collapse_key` |
| *no title and no body* | background push (`content-available: 1`, priority 5) | data-only message (Android priority `high`) |

¹ On iOS an image is attached on-device by a Notification Service Extension, which runs only for a
mutable alert; the dispatcher passes the URL as custom data and marks the push mutable.

A notification whose platform has no registered provider returns `PushResult.NotConfigured(platform)`
(not a throw), so a mixed batch keeps going and the failure is visible in the result.

## Building & packing

```bash
dotnet build Notifications/NotificationDispatcher/NotificationDispatcher.csproj
```

```bash
dotnet pack Notifications/NotificationDispatcher/NotificationDispatcher.csproj -c Release
```

Both projects target `net10.0`. `NotificationDispatcher` references `NotificationCore` plus the two
provider packages (`APNService`, `FCMService`). `NotificationCore` is a provider-agnostic contract
that is not published on its own — its assembly is bundled into the `NotificationDispatcher` package,
and `APNService` / `FCMService` are carried as package dependencies.
