using APNCore;
using APNCore.Models;
using Sut = NotificationDispatcher.NotificationDispatcher;

namespace NotificationDispatcher.Tests;

/// <summary>
/// Exercises the APNs mapping through the public dispatcher surface by capturing the
/// <see cref="ApnNotification"/> handed to the client. The mapper is internal, so this verifies its
/// behaviour as observed by callers rather than reaching into it directly.
/// </summary>
public class ApnMappingTests
{
    private static async Task<ApnNotification> Capture(PushNotification notification)
    {
        var apn = new FakeApnClient(Responses.ApnSuccess());
        var dispatcher = new Sut(apn, fcmClient: null);
        await dispatcher.SendAsync(notification);
        Assert.True(apn.Received.TryDequeue(out var sent));
        return sent!;
    }

    [Fact]
    public async Task Alert_MapsTokenTitleBodyBadgeSound_AsHighPriorityAlert()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Apple,
            DeviceToken = "device-abc",
            Title = "Title",
            Body = "Body",
            Badge = 7,
            Sound = "chime.caf",
            ApnsTopic = "com.example.app",
            CollapseId = "group-1",
        });

        Assert.Equal("device-abc", sent.DeviceToken);
        Assert.Equal(ApnPushType.Alert, sent.PushType);
        Assert.Equal(ApnPriority.High, sent.Priority);
        Assert.Equal("com.example.app", sent.Topic);
        Assert.Equal("group-1", sent.CollapseId);

        var aps = sent.Payload.Aps;
        Assert.NotNull(aps.Alert);
        Assert.Equal("Title", aps.Alert!.Title);
        Assert.Equal("Body", aps.Alert.Body);
        Assert.Equal(7, aps.Badge);
        Assert.Equal("chime.caf", aps.Sound);
        Assert.Null(aps.ContentAvailable);
        Assert.Null(aps.MutableContent);
    }

    [Fact]
    public async Task Alert_WithImageUrl_SetsMutableContentAndImageCustomData()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Apple,
            DeviceToken = "tok",
            Title = "Title",
            Body = "Body",
            ImageUrl = "https://cdn.example.com/pic.jpg",
        });

        Assert.Equal(1, sent.Payload.Aps.MutableContent);
        Assert.NotNull(sent.Payload.CustomData);
        Assert.Equal("https://cdn.example.com/pic.jpg", sent.Payload.CustomData!["image_url"]);
    }

    [Fact]
    public async Task Alert_WithoutImage_HasNoMutableContentAndNoCustomData()
    {
        var sent = await Capture(PushNotification.Apple("tok", "Title", "Body"));

        Assert.Null(sent.Payload.Aps.MutableContent);
        Assert.Null(sent.Payload.CustomData);
    }

    [Fact]
    public async Task Silent_MapsToBackgroundMediumPriorityWithContentAvailableAndNoAlert()
    {
        var sent = await Capture(PushNotification.DataOnly(
            DevicePlatform.Apple,
            "tok",
            new Dictionary<string, string> { ["k"] = "v" }));

        Assert.Equal(ApnPushType.Background, sent.PushType);
        Assert.Equal(ApnPriority.Medium, sent.Priority);
        Assert.Equal(1, sent.Payload.Aps.ContentAvailable);
        Assert.Null(sent.Payload.Aps.Alert);
    }

    [Fact]
    public async Task Data_IsCopiedIntoCustomData()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Apple,
            DeviceToken = "tok",
            Title = "Title",
            Data = new Dictionary<string, string> { ["order"] = "123", ["type"] = "receipt" },
        });

        Assert.NotNull(sent.Payload.CustomData);
        Assert.Equal("123", sent.Payload.CustomData!["order"]);
        Assert.Equal("receipt", sent.Payload.CustomData["type"]);
    }

    [Fact]
    public async Task Data_AndImage_AreBothPresentInCustomData()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Apple,
            DeviceToken = "tok",
            Title = "Title",
            ImageUrl = "https://cdn.example.com/pic.jpg",
            Data = new Dictionary<string, string> { ["k"] = "v" },
        });

        Assert.Equal("v", sent.Payload.CustomData!["k"]);
        Assert.Equal("https://cdn.example.com/pic.jpg", sent.Payload.CustomData["image_url"]);
    }
}
