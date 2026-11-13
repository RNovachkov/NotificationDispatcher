using APNCore;
using FCMCore;

namespace NotificationDispatcher.Tests.TestSupport;

/// <summary>
/// Factory helpers for the provider response records the fakes return. <c>ShouldRemoveToken</c> is a
/// computed, get-only property on both responses, so these helpers choose real reason/code values that
/// are known to drive it (e.g. APNs <c>Unregistered</c>, FCM <c>UNREGISTERED</c>).
/// </summary>
internal static class Responses
{
    // -- APNs -----------------------------------------------------------------------------------

    public static ApnResponse ApnSuccess(string apnsId = "apns-id-123") => new()
    {
        IsSuccess = true,
        StatusCode = 200,
        ApnsId = apnsId,
    };

    public static ApnResponse ApnFailure(int statusCode = 400, string reason = "BadMessageId") => new()
    {
        IsSuccess = false,
        StatusCode = statusCode,
        Reason = reason,
    };

    /// <summary>A failure whose reason makes <c>ShouldRemoveToken</c> true (410 / Unregistered).</summary>
    public static ApnResponse ApnUnregistered() => new()
    {
        IsSuccess = false,
        StatusCode = 410,
        Reason = "Unregistered",
    };

    // -- FCM ------------------------------------------------------------------------------------

    public static FcmResponse FcmSuccess(string messageName = "projects/p/messages/42") => new()
    {
        IsSuccess = true,
        StatusCode = 200,
        MessageName = messageName,
    };

    public static FcmResponse FcmFailure(
        int statusCode = 400,
        string errorStatus = "INVALID_ARGUMENT",
        string? errorCode = null,
        string? reason = null) => new()
    {
        IsSuccess = false,
        StatusCode = statusCode,
        ErrorStatus = errorStatus,
        ErrorCode = errorCode!,
        Reason = reason!,
    };

    /// <summary>A failure whose code makes <c>ShouldRemoveToken</c> true (UNREGISTERED).</summary>
    public static FcmResponse FcmUnregistered() => new()
    {
        IsSuccess = false,
        StatusCode = 404,
        ErrorStatus = "NOT_FOUND",
        ErrorCode = "UNREGISTERED",
    };
}
