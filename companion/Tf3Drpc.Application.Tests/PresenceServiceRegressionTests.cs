using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Tf3Drpc.Application.Services;
using Tf3Drpc.Application.Services.Interfaces;
using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Monitor;
using Tf3Drpc.Core.Resources;
using Tf3Drpc.Core.Settings;
using Tf3Drpc.Core.Structs;
using Tf3Drpc.Core.Translation;

using Xunit;

namespace Tf3Drpc.Application.Tests;

/// <summary>
/// Exercises presence and preview behaviour through a monitor tick on a test-owned log fixture.
/// </summary>
public sealed class PresenceServiceRegressionTests : IDisposable
{
    private readonly MonitorFixture _fixture = new();

    [Fact]
    public async Task InvalidPresetSetting_OnAPlayingGame_FallsBackToCareerAsync()
    {
        // The playing branch is where the preset matters; rank 2 renders "Mechanic" for Career.
        _fixture.Settings.Current = new AppSettings() { Preset = "unknown" };
        _fixture.WriteLog(rank: 2);

        AppStatusStruct status = await _fixture.TickAsync();

        Assert.Equal(GameState.Playing, status.Game.State);
        Assert.Equal("Mechanic", status.PresenceDetails);
        Assert.NotNull(_fixture.Sink.Last);
        Assert.Equal("Mechanic", _fixture.Sink.Last?.Details);
    }

    [Fact]
    public async Task PreviewAfterTryBuild_UsesTheRememberedStatusAsync()
    {
        // Rank 5 renders "Expert"; the preview's example status is rank 2, so a missed Remember would show "Mechanic".
        _fixture.WriteLog(rank: 5);

        AppStatusStruct status = await _fixture.TickAsync();
        LanguageInfo language = _fixture.Translation.Resolve("en", null);

        PresenceContentStruct preview = _fixture.PresenceService.BuildPreview(language, PresencePreset.Career, new PresenceTemplates(), out bool usesExample);

        Assert.Equal("Expert", status.PresenceDetails);
        Assert.False(usesExample);
        Assert.Equal("Expert", preview.Details);
    }

    [Fact]
    public async Task DistinctChosenAndGameTexts_AreUsedForTheirOwnPartsAsync()
    {
        // The game-language texts name the unsaved game; the chosen texts translate it into the presence.
        _fixture.Settings.Current = new AppSettings()
        {
            Language = "de",
            Preset = "custom",
            Templates = new PresenceTemplates() { Details = "{saveName}" }
        };

        _fixture.Translation.ChosenTexts = GameTexts.English;

        Dictionary<string, string> gameTexts = new(StringComparer.Ordinal) { ["New Game"] = "Neues Spiel" };
        _fixture.Translation.GameLanguageTexts = new GameTexts(gameTexts);
        _fixture.WriteLog(saveName: "Neues Spiel");

        AppStatusStruct status = await _fixture.TickAsync();

        Assert.Equal(GameState.Playing, status.Game.State);
        Assert.Equal("New Game", status.PresenceDetails);
    }

    public void Dispose()
        => _fixture.Dispose();

    private sealed class MonitorFixture : IDisposable
    {
        // Real session.log lines; only the timestamp shifts per test.
        private const string StartupLine = "[2026-10-13 21:20:43Z - MESSAGE  - Main             - Main           ]  Application startup. Hello!";

        private const string GameReadyLine = "[2026-10-13 21:15:24Z - MESSAGE  - Main Thread      - Main           ]  Game is ready";

        private const string StatusLine = "[2026-10-13 21:15:25Z - MESSAGE  - Main Thread      - Main           ]  [drpc-status] ";

        private readonly string _directory;
        private readonly string _logPath;

        public MonitorFixture()
        {
            Clock = new TestClock(DateTimeOffset.UtcNow);
            Settings = new TestSettings();
            Translation = new TestTranslation();
            Sink = new TestPresenceSink();
            PresenceService = new(Translation, Settings, Clock);
            _directory = Directory.CreateTempSubdirectory("tf3drpc-q1-").FullName;
            _logPath = Path.Combine(_directory, "stdout.txt");
            string profilePath = Path.Combine(_directory, "profile.lua");

            File.WriteAllText(profilePath, "-- test profile\n");
            GamePathsStruct paths = new() { LogPath = _logPath, ProfilePath = profilePath, GameDirectory = _directory };

            Monitor = new GameMonitor
            (
                new TestProcessSource(Clock.Now - TimeSpan.FromSeconds(13)),
                new TestPathResolver(paths),
                Settings,
                Translation,
                PresenceService,
                Sink,
                Clock,
                NullLogger<GameMonitor>.Instance
            );
        }

        public TestClock Clock { get; }

        public TestSettings Settings { get; }

        public TestTranslation Translation { get; }

        public TestPresenceSink Sink { get; }

        public PresenceService PresenceService { get; }

        public GameMonitor Monitor { get; }

        /// <summary>
        /// Writes startup, ready and one status line, timed against the fixture clock.
        /// </summary>
        public void WriteLog(int? rank = null, string? saveName = null)
        {
            StringBuilder json = new();

            json.Append("{\"v\":1");

            if (rank is int value)
                json.Append(",\"rank\":").Append(value.ToString(CultureInfo.InvariantCulture));

            json.Append(",\"speedup\":4,\"isMapEditor\":false,\"isSandbox\":false,\"isTycoon\":false");

            if (saveName is not null)
                json.Append(",\"saveName\":").Append(JsonString(saveName));

            json.Append('}');

            List<string> lines =
            [
                Shift(StartupLine, Clock.Now - TimeSpan.FromSeconds(12)),
                Shift(GameReadyLine, Clock.Now - TimeSpan.FromSeconds(11)),
                Shift(StatusLine + json, Clock.Now - TimeSpan.FromSeconds(10))
            ];

            // The real game log is CRLF; keep the fixture byte-compatible with it.
            string text = string.Empty;

            for (int i = 0; i < lines.Count; i++)
            {
                text += lines[i] + "\r\n";
            }

            File.WriteAllText(_logPath, text);
        }

        public async Task<AppStatusStruct> TickAsync()
            => (await Monitor.TickAsync(CancellationToken.None)) ?? Monitor.Status;

        public void Dispose()
            => Directory.Delete(_directory, recursive: true);

        // The log lines are CRLF in the real game; keep the fixture byte-compatible.
        private static string Shift(string template, in DateTimeOffset time)
            => $"[{time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}Z" +
                template[(template.IndexOf('Z', StringComparison.Ordinal) + 1)..];

        private static string JsonString(string text)
        {
            StringBuilder escaped = new(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c is '"' or '\\')
                    escaped.Append('\\');

                escaped.Append(c);
            }

            return '"' + escaped.ToString() + '"';
        }
    }

    private sealed class TestClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; } = now;

        public override long TimestampFrequency
            => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow()
            => Now;

        // The fixture never advances its clock; relative ages come from the log timestamps.
        public override long GetTimestamp()
            => 0;
    }

    private sealed class TestProcessSource(DateTimeOffset startedAt) : IGameProcessSource
    {
        public GameProcessInfoStruct Check()
            => new()
            {
                IsRunning = true,
                ExecutablePath = "/tmp/tf3drpc-test/game",
                StartTime = startedAt
            };
    }

    private sealed class TestPathResolver(GamePathsStruct paths) : IGamePathResolver
    {
        public GamePathsStruct ResolveGamePaths(string? runningExecutablePath)
            => paths; // The fixture resolves the same paths on every probe.
    }

    private sealed class TestPresenceSink : IPresenceSink
    {
        public bool IsConnected
            => false;

        public PresenceContentStruct? Last { get; private set; }

        public Task UpdateAsync(PresenceContentStruct? desired, CancellationToken cancellationToken)
        {
            Last = desired;

            return Task.CompletedTask;
        }
    }

    private sealed class TestSettings : ISettingsService
    {
        public AppSettings Current { get; set; } = new();

        public bool CanSave
            => false;

        public bool LastSaveFailed
            => false;

        public bool SetLanguage(string language)
            => throw new NotSupportedException();

        public bool SetPreset(PresencePreset preset)
            => throw new NotSupportedException();

        public bool SetTheme(ThemeMode theme)
            => throw new NotSupportedException();

        public bool SetTemplates(PresenceTemplates templates)
            => throw new NotSupportedException();

        public bool SetStoreButton(bool enabled)
            => throw new NotSupportedException();
    }

    private sealed class TestTranslation : ITranslationService
    {
        public IReadOnlyList<LanguageInfo> Languages
            => LanguageCatalog.All;

        public GameTexts ChosenTexts { get; set; } = GameTexts.English;

        public GameTexts GameLanguageTexts { get; set; } = GameTexts.English;

        public GameTexts GameTexts
            => ChosenTexts;

        public LanguageInfo Resolve(string setting, string? gameLanguageCode)
            => LanguageCatalog.Resolve(setting, gameLanguageCode);

        public string? ReadGameLanguageCode(string? profilePath)
            => null;

        public void LoadGameTexts(string? gameDirectory, LanguageInfo language, LanguageInfo gameLanguage)
        {
            if (!Equals(gameLanguage, language))
                return;

            GameLanguageTexts = ChosenTexts;
        }

        public string GetOwnText(OwnText text, LanguageInfo language)
            => OwnTexts.Get(text, language.Culture);
    }
}
