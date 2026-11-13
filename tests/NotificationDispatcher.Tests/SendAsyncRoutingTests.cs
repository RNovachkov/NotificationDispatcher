using Sut = NotificationDispatcher.NotificationDispatcher;

namespace NotificationDispatcher.Tests;

public class SendAsyncRoutingTests
{
    [Fact]
    public async Task SendAsync_ApplePlatform_RoutesToApnClientOnly()
    {
        var apn = new FakeApnClient(Responses.ApnSuccess());
        var fcm = new FakeFcmClient();
        var dispatcher = new Sut(apn, fcm);

        var result = await dispatcher.SendAsync(PushNotification.Apple("tok", "Hi", "There"));

        Assert.Equal(1, apn.CallCount);
        Assert.Equal(0, fcm.CallCount);
        Assert.True(result.IsSuccess);
        Assert.Equal(DevicePlatform.Apple, result.Platform);
    }

    [Fact]
    public async Task SendAsync_AndroidPlatform_RoutesToFcmClientOnly()
    {
        var apn = new FakeApnClient();
        var fcm = new FakeFcmClient(Responses.FcmSuccess());
        var dispatcher = new Sut(apn, fcm);

        var result = await dispatcher.SendAsync(PushNotification.Android("tok", "Hi", "There"));

        Assert.Equal(0, apn.CallCount);
        Assert.Equal(1, fcm.CallCount);
        Assert.True(result.IsSuccess);
        Assert.Equal(DevicePlatform.Android, result.Platform);
    }

    [Fact]
    public async Task SendAsync_ApplePush_WhenApnNotConfigured_ReturnsNotConfigured()
    {
        var dispatcher = new Sut(apnClient: null, new FakeFcmClient());

        var result = await dispatcher.SendAsync(PushNotification.Apple("tok", "Hi", "There"));

        Assert.False(result.IsSuccess);
        Assert.Equal(DevicePlatform.Apple, result.Platform);
        Assert.Equal("ProviderNotConfigured", result.ErrorCode);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public async Task SendAsync_AndroidPush_WhenFcmNotConfigured_ReturnsNotConfigured()
    {
        var dispatcher = new Sut(new FakeApnClient(), fcmClient: null);

        var result = await dispatcher.SendAsync(PushNotification.Android("tok", "Hi", "There"));

        Assert.False(result.IsSuccess);
        Assert.Equal(DevicePlatform.Android, result.Platform);
        Assert.Equal("ProviderNotConfigured", result.ErrorCode);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public async Task SendAsync_NullNotification_ThrowsArgumentNullException()
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.SendAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_BlankDeviceToken_ThrowsArgumentException(string token)
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());
        var notification = new PushNotification
        {
            Platform = DevicePlatform.Apple,
            DeviceToken = token,
            Title = "Hi",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.SendAsync(notification));
    }

    [Fact]
    public async Task SendAsync_UnknownPlatform_ThrowsArgumentOutOfRange()
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());
        var notification = new PushNotification
        {
            Platform = (DevicePlatform)999,
            DeviceToken = "tok",
            Title = "Hi",
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dispatcher.SendAsync(notification));
    }

    [Fact]
    public async Task SendAsync_PassesCancellationTokenThroughToClient()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var apn = new FakeApnClient(Responses.ApnSuccess());
        var dispatcher = new Sut(apn, fcmClient: null);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.SendAsync(PushNotification.Apple("tok", "Hi", "There"), cts.Token));
    }
}
