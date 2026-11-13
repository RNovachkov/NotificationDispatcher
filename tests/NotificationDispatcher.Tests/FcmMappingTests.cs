using FCMCore;
using FCMCore.Models;
using Sut = NotificationDispatcher.NotificationDispatcher;

namespace NotificationDispatcher.Tests;

/// <summary>
/// Exercises the FCM mapping through the public dispatcher surface by capturing the
/// <see cref="FcmMessage"/> handed to the client.
/// </summary>
public class FcmMappingTests
{
    private static async Task<FcmMessage> Capture(PushNotification notification)
    {
        var fcm = new FakeFcmClient(Responses.FcmSuccess());
        var dispatcher = new Sut(apnClient: null, fcm);
        await dispatcher.SendAsync(notification);
        Assert.True(fcm.Received.TryDequeue(out var sent));
        return sent!;
    }

    [Fact]
    public async Task Alert_MapsTokenTitleBody_AsNotification()
    {
        var sent = await Capture(PushNotification.Android("device-xyz", "Title", "Body"));

        Assert.Equal("device-xyz", sent.Token);
        Assert.NotNull(sent.Notification);
        Assert.Equal("Title", sent.Notification!.Title);
        Assert.Equal("Body", sent.Notification.Body);
    }

    [Fact]
    public async Task Alert_WithNoCollapseOrSound_HasNoAndroidConfig()
    {
        var sent = await Capture(PushNotification.Android("tok", "Title", "Body"));

        Assert.Null(sent.Android);
    }

    [Fact]
    public async Task Alert_WithImageUrl_SetsNotificationImage()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Android,
            DeviceToken = "tok",
            Title = "Title",
            ImageUrl = "https://cdn.example.com/pic.jpg",
        });

        Assert.Equal("https://cdn.example.com/pic.jpg", sent.Notification!.Image);
    }

    [Fact]
    public async Task Alert_WithSound_PutsSoundOnAndroidNotification()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Android,
            DeviceToken = "tok",
            Title = "Title",
            Sound = "chime",
        });

        Assert.NotNull(sent.Android);
        Assert.NotNull(sent.Android!.Notification);
        Assert.Equal("chime", sent.Android.Notification!.Sound);
    }

    [Fact]
    public async Task Alert_WithCollapseId_PutsCollapseKeyOnAndroidConfig()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Android,
            DeviceToken = "tok",
            Title = "Title",
            CollapseId = "group-7",
        });

        Assert.NotNull(sent.Android);
        Assert.Equal("group-7", sent.Android!.CollapseKey);
    }

    [Fact]
    public async Task Silent_MapsToDataOnlyMessageWithNoNotificationAndHighAndroidPriority()
    {
        var sent = await Capture(PushNotification.DataOnly(
            DevicePlatform.Android,
            "tok",
            new Dictionary<string, string> { ["k"] = "v" }));

        Assert.Null(sent.Notification);
        Assert.NotNull(sent.Data);
        Assert.Equal("v", sent.Data!["k"]);
        Assert.NotNull(sent.Android);
        Assert.Equal(AndroidMessagePriority.High, sent.Android!.Priority);
    }

    [Fact]
    public async Task Data_IsCopiedOntoMessage()
    {
        var sent = await Capture(new PushNotification
        {
            Platform = DevicePlatform.Android,
            DeviceToken = "tok",
            Title = "Title",
            Data = new Dictionary<string, string> { ["order"] = "123" },
        });

        Assert.NotNull(sent.Data);
        Assert.Equal("123", sent.Data!["order"]);
    }

    [Fact]
    public async Task NoData_LeavesMessageDataNull()
    {
        var sent = await Capture(PushNotification.Android("tok", "Title", "Body"));

        Assert.Null(sent.Data);
    }
}
