using Sut = NotificationDispatcher.NotificationDispatcher;

namespace NotificationDispatcher.Tests;

public class SendManyAsyncTests
{
    [Fact]
    public async Task SendManyAsync_ReturnsResultsInInputOrder_AcrossPlatforms()
    {
        // Each client echoes the device token back as the message id, so a result can be tied to the
        // notification that produced it regardless of the order the concurrent sends complete in.
        var apn = new FakeApnClient(n => Responses.ApnSuccess(apnsId: n.DeviceToken));
        var fcm = new FakeFcmClient(m => Responses.FcmSuccess(messageName: m.Token!));
        var dispatcher = new Sut(apn, fcm);

        var notifications = new List<PushNotification>();
        for (var i = 0; i < 50; i++)
        {
            var platform = i % 2 == 0 ? DevicePlatform.Apple : DevicePlatform.Android;
            notifications.Add(PushNotification.Alert(platform, $"token-{i}", "T", "B"));
        }

        var results = await dispatcher.SendManyAsync(notifications, maxConcurrency: 8);

        Assert.Equal(notifications.Count, results.Count);
        for (var i = 0; i < notifications.Count; i++)
        {
            Assert.True(results[i].IsSuccess);
            Assert.Equal(notifications[i].Platform, results[i].Platform);
            // MessageId carries the echoed token, proving result[i] corresponds to notifications[i].
            Assert.Equal($"token-{i}", results[i].MessageId);
        }
    }

    [Fact]
    public async Task SendManyAsync_NeverExceedsMaxConcurrency()
    {
        const int maxConcurrency = 4;
        // A delay forces calls to overlap so the gate is actually exercised.
        var apn = new FakeApnClient(Responses.ApnSuccess(), delay: TimeSpan.FromMilliseconds(25));
        var dispatcher = new Sut(apn, fcmClient: null);

        var notifications = Enumerable.Range(0, 40)
            .Select(i => PushNotification.Apple($"token-{i}", "T", "B"))
            .ToList();

        await dispatcher.SendManyAsync(notifications, maxConcurrency);

        Assert.Equal(notifications.Count, apn.CallCount);
        Assert.True(
            apn.MaxObservedConcurrency <= maxConcurrency,
            $"observed concurrency {apn.MaxObservedConcurrency} exceeded limit {maxConcurrency}");
    }

    [Fact]
    public async Task SendManyAsync_RoutesEachNotificationToItsPlatformProvider()
    {
        var apn = new FakeApnClient(Responses.ApnSuccess());
        var fcm = new FakeFcmClient(Responses.FcmSuccess());
        var dispatcher = new Sut(apn, fcm);

        var notifications = new List<PushNotification>
        {
            PushNotification.Apple("a1", "T", "B"),
            PushNotification.Android("g1", "T", "B"),
            PushNotification.Android("g2", "T", "B"),
            PushNotification.Apple("a2", "T", "B"),
        };

        await dispatcher.SendManyAsync(notifications);

        Assert.Equal(2, apn.CallCount);
        Assert.Equal(2, fcm.CallCount);
    }

    [Fact]
    public async Task SendManyAsync_WithUnconfiguredProvider_ReturnsNotConfiguredForThoseEntries()
    {
        // Only APNs configured; Android entries must come back as NotConfigured, not throw.
        var dispatcher = new Sut(new FakeApnClient(Responses.ApnSuccess()), fcmClient: null);

        var notifications = new List<PushNotification>
        {
            PushNotification.Apple("a1", "T", "B"),
            PushNotification.Android("g1", "T", "B"),
        };

        var results = await dispatcher.SendManyAsync(notifications);

        Assert.True(results[0].IsSuccess);
        Assert.False(results[1].IsSuccess);
        Assert.Equal("ProviderNotConfigured", results[1].ErrorCode);
    }

    [Fact]
    public async Task SendManyAsync_EmptyList_ReturnsEmpty()
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());

        var results = await dispatcher.SendManyAsync(Array.Empty<PushNotification>());

        Assert.Empty(results);
    }

    [Fact]
    public async Task SendManyAsync_NullList_ThrowsArgumentNullException()
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.SendManyAsync(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SendManyAsync_NonPositiveConcurrency_ThrowsArgumentOutOfRange(int maxConcurrency)
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());
        var notifications = new List<PushNotification> { PushNotification.Apple("a1", "T", "B") };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => dispatcher.SendManyAsync(notifications, maxConcurrency));
    }
}
