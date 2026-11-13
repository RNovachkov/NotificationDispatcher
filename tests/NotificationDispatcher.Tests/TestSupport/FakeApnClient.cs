using System.Collections.Concurrent;
using APNCore;
using APNCore.Models;

namespace NotificationDispatcher.Tests.TestSupport;

/// <summary>
/// A hand-rolled <see cref="IApnClient"/> test double. It records every notification it is handed
/// (in call order), returns a configurable response, and tracks concurrency so tests can assert the
/// dispatcher's gating. No mocking framework is used, to keep the dependency surface small and the
/// concurrency behaviour explicit.
/// </summary>
internal sealed class FakeApnClient : IApnClient
{
    private readonly Func<ApnNotification, ApnResponse> _responder;
    private readonly TimeSpan _delay;

    // Concurrency tracking.
    private int _inFlight;
    private int _maxObservedConcurrency;

    public FakeApnClient(ApnResponse? response = null, TimeSpan? delay = null)
        : this(_ => response ?? Responses.ApnSuccess(), delay)
    {
    }

    public FakeApnClient(Func<ApnNotification, ApnResponse> responder, TimeSpan? delay = null)
    {
        _responder = responder;
        _delay = delay ?? TimeSpan.Zero;
    }

    /// <summary>Every notification passed to <see cref="SendAsync"/>, in the order received.</summary>
    public ConcurrentQueue<ApnNotification> Received { get; } = new();

    /// <summary>Number of times <see cref="SendAsync"/> was invoked.</summary>
    public int CallCount => Received.Count;

    /// <summary>The greatest number of <see cref="SendAsync"/> calls observed in flight at once.</summary>
    public int MaxObservedConcurrency => Volatile.Read(ref _maxObservedConcurrency);

    public async Task<ApnResponse> SendAsync(ApnNotification notification, CancellationToken cancellationToken = default)
    {
        var now = Interlocked.Increment(ref _inFlight);
        UpdateMax(now);
        try
        {
            Received.Enqueue(notification);
            cancellationToken.ThrowIfCancellationRequested();
            if (_delay > TimeSpan.Zero)
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return _responder(notification);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public Task<IReadOnlyList<ApnResponse>> SendManyAsync(
        IReadOnlyList<ApnNotification> notifications,
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default)
        // The dispatcher never calls this (it fans out over SendAsync itself); implemented only to
        // satisfy the interface.
        => throw new NotSupportedException("The dispatcher does not use IApnClient.SendManyAsync.");

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
