using System.Collections.Concurrent;
using ConCancha.Infrastructure.Email;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Infrastructure.Email;

public class EmailDispatcherTests
{
    private const string SecretBody = "Link privado: https://club.test/panel/restablecer?token=secreto";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly OutgoingEmail Sample = new("ana@club.com", "Asunto", SecretBody);

    private readonly EmailQueue _queue = new();
    private readonly ImmediateTimeProvider _time = new();
    private readonly ListLogger<EmailDispatcher> _logger = new();

    [Fact]
    public async Task QueuedSender_ReturnsBeforeTheProviderAnswers()
    {
        var transport = new ScriptedTransport(blockUntilReleased: true);
        await using var running = await StartAsync(transport);

        await new QueuedEmailSender(_queue).SendAsync(Sample.To, Sample.Subject, Sample.Body, CancellationToken.None);

        transport.Release();
        Assert.Equal(Sample, await transport.Delivered.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task TransientFailures_AreRetriedWithTheConfiguredDelays()
    {
        var transport = new ScriptedTransport(transientFailures: 2);
        await using var running = await StartAsync(transport);

        _queue.Enqueue(Sample);

        await transport.Delivered.Task.WaitAsync(Timeout);
        Assert.Equal(3, transport.Attempts);
        Assert.Equal(EmailDispatcher.RetryDelays.Take(2), _time.Delays);
    }

    [Fact]
    public async Task PermanentFailure_IsNotRetriedAndTheNextMessageStillGoesOut()
    {
        var transport = new ScriptedTransport(permanentFailures: 1);
        await using var running = await StartAsync(transport);

        _queue.Enqueue(Sample);
        _queue.Enqueue(Sample with { To = "beto@club.com" });

        Assert.Equal("beto@club.com", (await transport.Delivered.Task.WaitAsync(Timeout)).To);
        Assert.Equal(2, transport.Attempts);
        Assert.Empty(_time.Delays);
    }

    [Fact]
    public async Task GivingUp_AfterTheLastRetry()
    {
        var transport = new ScriptedTransport(transientFailures: int.MaxValue);
        await using var running = await StartAsync(transport);

        _queue.Enqueue(Sample);

        await WaitUntilAsync(() => _logger.Messages.Any(message => message.Contains("dropped")));
        Assert.Equal(EmailDispatcher.RetryDelays.Count + 1, transport.Attempts);
    }

    [Fact]
    public async Task Logs_NeverIncludeTheBodyOrTheFullAddress()
    {
        var transport = new ScriptedTransport(transientFailures: 1);
        await using var running = await StartAsync(transport);

        _queue.Enqueue(Sample);
        await transport.Delivered.Task.WaitAsync(Timeout);

        Assert.NotEmpty(_logger.Messages);
        Assert.All(_logger.Messages, message => Assert.DoesNotContain("secreto", message));
        Assert.All(_logger.Messages, message => Assert.DoesNotContain(Sample.To, message));
    }

    private async Task<RunningDispatcher> StartAsync(IEmailTransport transport)
    {
        var dispatcher = new EmailDispatcher(_queue, transport, _time, _logger);
        await dispatcher.StartAsync(CancellationToken.None);
        return new RunningDispatcher(dispatcher);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        while (!condition())
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
    }

    private sealed class RunningDispatcher(EmailDispatcher dispatcher) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await dispatcher.StopAsync(CancellationToken.None);
            dispatcher.Dispose();
        }
    }

    private sealed class ScriptedTransport(int transientFailures = 0, int permanentFailures = 0, bool blockUntilReleased = false)
        : IEmailTransport
    {
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attempts;

        public TaskCompletionSource<OutgoingEmail> Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Attempts => _attempts;

        public void Release() => _released.TrySetResult();

        public async Task DeliverAsync(OutgoingEmail email, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _attempts);
            if (blockUntilReleased) await _released.Task.WaitAsync(cancellationToken);
            if (attempt <= permanentFailures) throw new EmailDeliveryException("rejected", isTransient: false);
            if (attempt <= permanentFailures + (long)transientFailures) throw new EmailDeliveryException("down", isTransient: true);
            Delivered.TrySetResult(email);
        }
    }

    /// <summary>Fires every timer right away and records the requested delays, so retries run without waiting.</summary>
    private sealed class ImmediateTimeProvider : TimeProvider
    {
        private readonly ConcurrentQueue<TimeSpan> _delays = new();

        public IReadOnlyCollection<TimeSpan> Delays => _delays;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _delays.Enqueue(dueTime);
            return new ImmediateTimer(callback, state);
        }

        private sealed class ImmediateTimer : ITimer
        {
            public ImmediateTimer(TimerCallback callback, object? state) =>
                ThreadPool.QueueUserWorkItem(_ => callback(state));

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose() { }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
