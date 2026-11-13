using System.Collections.Concurrent;
using FCMCore;
using FCMCore.Models;

namespace NotificationDispatcher.Tests.TestSupport;

/// <summary>
/// A hand-rolled <see cref="IFcmClient"/> test double. Mirror of <see cref="FakeApnClient"/>: records
/// every message it is handed (in call order), returns a configurable response, and tracks concurrency.
/// </summary>
internal sealed class FakeFcmClient : IFcmClient
{
    private readonly Func<FcmMessage, FcmResponse> _responder;
    private readonly TimeSpan _delay;

    private int _inFlight;
    private int _maxObservedConcurrency;

    public FakeFcmClient(FcmResponse? response = null, TimeSpan? delay = null)
        : this(_ => response ?? Responses.FcmSuccess(), delay)
    {
    }

    public FakeFcmClient(Func<FcmMessage, FcmResponse> responder, TimeSpan? delay = null)
    {
        _responder = responder;
        _delay = delay ?? TimeSpan.Zero;
    }

    /// <summary>Every message passed to <see cref="SendAsync"/>, in the order received.</summary>
    public ConcurrentQueue<FcmMessage> Received { get; } = new();

    /// <summary>Number of times <see cref="SendAsync"/> was invoked.</summary>
    public int CallCount => Received.Count;

    /// <summary>The greatest number of <see cref="SendAsync"/> calls observed in flight at once.</summary>
    public int MaxObservedConcurrency => Volatile.Read(ref _maxObservedConcurrency);

    public async Task<FcmResponse> SendAsync(FcmMessage message, CancellationToken cancellationToken = default)
    {
        var now = Interlocked.Increment(ref _inFlight);
        UpdateMax(now);
        try
        {
            Received.Enqueue(message);
            cancellationToken.ThrowIfCancellationRequested();
            if (_delay > TimeSpan.Zero)
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return _responder(message);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public Task<IReadOnlyList<FcmResponse>> SendManyAsync(
        IReadOnlyList<FcmMessage> messages,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The dispatcher does not use IFcmClient.SendManyAsync.");

    private void UpdateMax(int candidate)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref _maxObservedConcurrency);
            if (candidate <= observed)
                return;
        }
        while (Interlocked.CompareExchange(ref _maxObservedConcurrency, candidate, observed) != observed);
    }
}
