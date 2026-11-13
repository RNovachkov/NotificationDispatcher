using Sut = NotificationDispatcher.NotificationDispatcher;

namespace NotificationDispatcher.Tests;

public class ConstructorAndSupportsTests
{
    [Fact]
    public void Constructor_WithBothClients_SupportsBothPlatforms()
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());

        Assert.True(dispatcher.SupportsApple);
        Assert.True(dispatcher.SupportsAndroid);
        Assert.True(dispatcher.Supports(DevicePlatform.Apple));
        Assert.True(dispatcher.Supports(DevicePlatform.Android));
    }

    [Fact]
    public void Constructor_WithOnlyApn_SupportsAppleOnly()
    {
        var dispatcher = new Sut(new FakeApnClient(), fcmClient: null);

        Assert.True(dispatcher.SupportsApple);
        Assert.False(dispatcher.SupportsAndroid);
        Assert.True(dispatcher.Supports(DevicePlatform.Apple));
        Assert.False(dispatcher.Supports(DevicePlatform.Android));
    }

    [Fact]
    public void Constructor_WithOnlyFcm_SupportsAndroidOnly()
    {
        var dispatcher = new Sut(apnClient: null, new FakeFcmClient());

        Assert.False(dispatcher.SupportsApple);
        Assert.True(dispatcher.SupportsAndroid);
        Assert.False(dispatcher.Supports(DevicePlatform.Apple));
        Assert.True(dispatcher.Supports(DevicePlatform.Android));
    }

    [Fact]
    public void Constructor_WithNoClients_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Sut(apnClient: null, fcmClient: null));
        Assert.Contains("At least one provider", ex.Message);
    }

    [Fact]
    public void Supports_WithUndefinedPlatform_ReturnsFalse()
    {
        var dispatcher = new Sut(new FakeApnClient(), new FakeFcmClient());

        // A value outside the defined enum members resolves to the switch default.
        Assert.False(dispatcher.Supports((DevicePlatform)999));
    }
}
