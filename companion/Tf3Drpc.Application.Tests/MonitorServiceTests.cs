using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.Logging.Abstractions;

using Tf3Drpc.Application.Messages;
using Tf3Drpc.Application.Services;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Monitor;
using Tf3Drpc.Core.Settings;
using Tf3Drpc.Core.Structs;

using Xunit;

namespace Tf3Drpc.Application.Tests;

public sealed class MonitorServiceTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task StartTicksImmediatelyAndOnlyOnceThenStopAllowsRestartAsync()
    {
        await using Fixture fixture = new();
        Assert.NotNull(fixture.Service.Status.Language);
        await fixture.Service.StopAsync();
        Assert.Equal(0, fixture.Clock.Created);

        fixture.Service.Start();
        fixture.Service.Start();
        AppStatusChangedMessage first = await fixture.NextMessageAsync();
        Assert.True(first.Status.DiscordConnected);
        Assert.Equal(first.Status, fixture.Service.Status);
        Assert.Equal(1, fixture.Clock.Created);
        ManualTimer timer = fixture.Clock.Current;
        Assert.Equal(TimeSpan.FromSeconds(1), timer.DueTime);
        Assert.Equal(TimeSpan.FromSeconds(1), timer.Period);

        fixture.Sink.IsConnected = false;
        timer.Fire();
        AppStatusChangedMessage second = await fixture.NextMessageAsync();
        Assert.False(second.Status.DiscordConnected);
        Assert.Equal(2, fixture.Sink.Calls);

        await fixture.Service.StopAsync().WaitAsync(Deadline, TestContext.Current.CancellationToken);
        Assert.True(timer.IsDisposed);
        await fixture.Service.StopAsync();

        fixture.Sink.IsConnected = true;
        fixture.Service.Start();
        Assert.True((await fixture.NextMessageAsync()).Status.DiscordConnected);
        Assert.Equal(2, fixture.Clock.Created);
        Assert.Equal(3, fixture.Sink.Calls);
        await fixture.Service.DisposeAsync();
        Assert.True(fixture.Clock.Current.IsDisposed);
    }

    [Fact]
    public async Task StopCancelsAndWaitsForTheInFlightTickWithoutDisposingSinkAsync()
    {
        await using Fixture fixture = new();
        fixture.Sink.HoldTick = true;
        fixture.Service.Start();
        await fixture.Sink.Entered.Task.WaitAsync(Deadline, TestContext.Current.CancellationToken);

        Task stopping = fixture.Service.StopAsync();
        await fixture.Sink.Cancelled.Task.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        Assert.False(stopping.IsCompleted);
        Assert.Equal(1, fixture.Sink.Calls);

        fixture.Sink.Release.TrySetResult();
        await stopping.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        Assert.True(fixture.Clock.Current.IsDisposed);
        Assert.False(fixture.Sink.IsDisposed);
        Assert.False(fixture.Messages.Reader.TryRead(out _));
    }

    [Fact]
    public async Task AThrowingMessageReceiverDoesNotEndTheLoopAsync()
    {
        await using Fixture fixture = new();
        fixture.ThrowOnReceive = true;
        fixture.Service.Start();
        Assert.True((await fixture.NextMessageAsync()).Status.DiscordConnected);

        fixture.Sink.IsConnected = false;
        fixture.Clock.Current.Fire();
        Assert.False((await fixture.NextMessageAsync()).Status.DiscordConnected);
        Assert.Equal(2, fixture.Sink.Calls);
        await fixture.Service.StopAsync().WaitAsync(Deadline, TestContext.Current.CancellationToken);
        Assert.True(fixture.Clock.Current.IsDisposed);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture()
        {
            StrongReferenceMessenger messenger = new();
            messenger.Register<Fixture, AppStatusChangedMessage>(this, static (recipient, message) => recipient.Receive(message));
            Service = new MonitorService
            (
                new ProcessSource(),
                new PathResolver(),
                new SettingsSource(),
                new LanguageResolver(),
                new PresenceBuilder(),
                Sink,
                messenger,
                Clock,
                NullLoggerFactory.Instance
            );
        }

        public ManualClock Clock { get; } = new();

        public PresenceSink Sink { get; } = new();

        public Channel<AppStatusChangedMessage> Messages { get; } = Channel.CreateUnbounded<AppStatusChangedMessage>();

        public MonitorService Service { get; }

        public bool ThrowOnReceive { get; set; }

        public async Task<AppStatusChangedMessage> NextMessageAsync()
            => await Messages.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(Deadline, TestContext.Current.CancellationToken);

        public async ValueTask DisposeAsync()
        {
            // Release a held synthetic tick even when an assertion fails, so cleanup cannot strand the loop.
            Sink.Release.TrySetResult();
            await Service.DisposeAsync().AsTask().WaitAsync(Deadline);
        }

        private void Receive(AppStatusChangedMessage message)
        {
            Messages.Writer.TryWrite(message);

            if (ThrowOnReceive)
                throw new InvalidOperationException("Synthetic receiver failure.");
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private ManualTimer? _current;
        private int _created;

        public int Created
            => Volatile.Read(ref _created);

        public ManualTimer Current
            => Volatile.Read(ref _current) ?? throw new InvalidOperationException("No timer created.");

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ManualTimer timer = new(callback, state, dueTime, period);
            Volatile.Write(ref _current, timer);
            Interlocked.Increment(ref _created);

            return timer;
        }
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
    {
        private readonly TimerCallback _callback = callback;
        private readonly object? _state = state;
        private int _disposed;

        public TimeSpan DueTime { get; } = dueTime;

        public TimeSpan Period { get; } = period;

        public bool IsDisposed
            => Volatile.Read(ref _disposed) is not 0;

        public void Fire()
        {
            if (!IsDisposed)
                _callback(_state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
            => throw new NotSupportedException();

        public void Dispose()
            => Interlocked.Exchange(ref _disposed, 1);

        public ValueTask DisposeAsync()
        {
            Dispose();

            return ValueTask.CompletedTask;
        }
    }

    private sealed class PresenceSink : IPresenceSink, IAsyncDisposable
    {
        private int _calls;

        public bool IsConnected { get; set; } = true;

        public bool HoldTick { get; set; }

        public bool IsDisposed { get; private set; }

        public int Calls
            => Volatile.Read(ref _calls);

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task UpdateAsync(PresenceContentStruct? desired, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            await using CancellationTokenRegistration registration = cancellationToken.Register(() => Cancelled.TrySetResult());
            Entered.TrySetResult();

            if (HoldTick)
                await Release.Task;

            cancellationToken.ThrowIfCancellationRequested();
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;

            return ValueTask.CompletedTask;
        }
    }

    private sealed class ProcessSource : IGameProcessSource
    {
        public GameProcessInfoStruct Check()
            => default;
    }

    private sealed class PathResolver : IGamePathResolver
    {
        public GamePathsStruct ResolveGamePaths(string? runningExecutablePath)
            => default;
    }

    private sealed class SettingsSource : ISettingsSource
    {
        public AppSettings Current { get; } = new();
    }

    private sealed class LanguageResolver : ILanguageResolver
    {
        public LanguageInfo Resolve(string setting, string? gameLanguageCode)
            => LanguageCatalog.Resolve(setting, gameLanguageCode);

        public string? ReadGameLanguageCode(string? profilePath)
            => throw new InvalidOperationException("The fixture must not read a profile.");

        public void LoadGameTexts(string? gameDirectory, LanguageInfo language, LanguageInfo gameLanguage) { }
    }

    private sealed class PresenceBuilder : IPresenceBuilder
    {
        public bool TryBuild(in GameStateSnapshotStruct snapshot, LanguageInfo language, out PresenceContentStruct content)
        {
            content = default;

            return false;
        }
    }
}
