using APNCore;
using FCMCore;
using Sut = NotificationDispatcher.NotificationDispatcher;

namespace NotificationDispatcher.Tests;

/// <summary>
/// Verifies that provider responses are flattened onto <see cref="PushResult"/> correctly, as observed
/// from the dispatcher's return value.
/// </summary>
public class ResultMappingTests
{
    private static async Task<PushResult> SendApple(ApnResponse response)
    {
        var dispatcher = new Sut(new FakeApnClient(response), fcmClient: null);
        return await dispatcher.SendAsync(PushNotification.Apple("tok", "T", "B"));
    }

    private static async Task<PushResult> SendAndroid(FcmResponse response)
    {
        var dispatcher = new Sut(apnClient: null, new FakeFcmClient(response));
        return await dispatcher.SendAsync(PushNotification.Android("tok", "T", "B"));
    }

    // -- APNs -----------------------------------------------------------------------------------

    [Fact]
    public async Task ApnSuccess_MapsApnsIdToMessageId()
    {
        var response = Responses.ApnSuccess("apns-9");
        var result = await SendApple(response);

        Assert.True(result.IsSuccess);
        Assert.Equal(DevicePlatform.Apple, result.Platform);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("apns-9", result.MessageId);
        Assert.Null(result.ErrorCode);
        Assert.Same(response, result.ProviderResponse);
    }

    [Fact]
    public async Task ApnFailure_MapsReasonToErrorCodeAndMessage()
    {
        var result = await SendApple(Responses.ApnFailure(400, "BadDeviceToken"));

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("BadDeviceToken", result.ErrorCode);
        Assert.Equal("BadDeviceToken", result.ErrorMessage);
    }

    [Fact]
    public async Task ApnUnregistered_PropagatesShouldRemoveToken()
    {
        var result = await SendApple(Responses.ApnUnregistered());

        Assert.False(result.IsSuccess);
        Assert.True(result.ShouldRemoveToken);
    }

    // -- FCM ------------------------------------------------------------------------------------

    [Fact]
    public async Task FcmSuccess_MapsMessageNameToMessageId()
    {
        var response = Responses.FcmSuccess("projects/p/messages/7");
        var result = await SendAndroid(response);

        Assert.True(result.IsSuccess);
        Assert.Equal(DevicePlatform.Android, result.Platform);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("projects/p/messages/7", result.MessageId);
        Assert.Same(response, result.ProviderResponse);
    }

    [Fact]
    public async Task FcmFailure_PrefersErrorCodeOverErrorStatus()
    {
        var result = await SendAndroid(Responses.FcmFailure(
            statusCode: 400,
            errorStatus: "INVALID_ARGUMENT",
            errorCode: "mismatched-credential",
            reason: "Sender ID mismatch"));

        Assert.False(result.IsSuccess);
        Assert.Equal("mismatched-credential", result.ErrorCode);
        Assert.Equal("Sender ID mismatch", result.ErrorMessage);
    }

    [Fact]
    public async Task FcmFailure_FallsBackToErrorStatusWhenErrorCodeNull()
    {
        var result = await SendAndroid(Responses.FcmFailure(
            statusCode: 403,
            errorStatus: "PERMISSION_DENIED",
            errorCode: null));

        Assert.Equal("PERMISSION_DENIED", result.ErrorCode);
    }

    [Fact]
    public async Task FcmUnregistered_PropagatesShouldRemoveToken()
    {
        var result = await SendAndroid(Responses.FcmUnregistered());

        Assert.False(result.IsSuccess);
        Assert.True(result.ShouldRemoveToken);
    }
}
