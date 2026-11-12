using System;
using System.Collections.Generic;

using Tf3Drpc.Application.Services;
using Tf3Drpc.Application.Services.Interfaces;
using Tf3Drpc.Core.Constants;
using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Presence;
using Tf3Drpc.Core.Settings;
using Tf3Drpc.Core.Structs;
using Tf3Drpc.Core.Translation;

using Xunit;

namespace Tf3Drpc.Application.Tests;

public sealed class PresenceServiceTests
{
    [Fact]
    public void PreviewUsesCurrentSettingsAndElapsedMonotonicTime()
    {
        TestSettings settings = new();
        TestTranslation translation = new();
        TestClock clock = new();
        PresenceService service = new(translation, settings, clock);
        LanguageInfo language = translation.Resolve("en", null);
        PresencePreview expectedPreview = new();
        PresenceTemplates templates = new();

        for (int step = 0; step < 3; step++)
        {
            settings.Current = settings.Current with { StoreButton = step is 1 };
            clock.Timestamp = step * PresenceConstants.AlternatingInterval.Ticks;
            PresenceOptionsStruct options = new()
            {
                Preset = PresencePreset.Alternating,
                RotationStep = step,
                Templates = templates,
                ShowStoreButton = settings.Current.StoreButton,
                GameLanguageTexts = translation.GameLanguageTexts
            };
            PresenceContentStruct expected = expectedPreview.Build(language.Culture, translation.GameTexts, options, out _);
            PresenceContentStruct actual = service.BuildPreview(language, PresencePreset.Alternating, templates, out bool usesExample);

            Assert.True(usesExample);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void MainMenuPresenceMatchesCoreBuilder()
    {
        TestSettings settings = new() { Current = new AppSettings() { Preset = "unknown" } };
        TestTranslation translation = new();
        PresenceService service = new(translation, settings, new TestClock());
        LanguageInfo language = translation.Resolve("en", null);
        GameStateSnapshotStruct snapshot = new() { State = GameState.MainMenu };
        PresenceOptionsStruct options = new()
        {
            Preset = PresencePreset.Career,
            Templates = settings.Current.Templates,
            ShowStoreButton = settings.Current.StoreButton,
            GameLanguageTexts = translation.GameLanguageTexts
        };

        bool expectedResult = PresenceBuilder.TryBuild(snapshot, language.Culture, translation.GameTexts, options, out PresenceContentStruct expected);
        bool actualResult = service.TryBuild(snapshot, language, out PresenceContentStruct actual);

        Assert.Equal(expectedResult, actualResult);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullPresentationInputsAreRejected()
    {
        TestTranslation translation = new();
        PresenceService service = new(translation, new TestSettings(), new TestClock());
        LanguageInfo language = translation.Resolve("en", null);

        Assert.Throws<ArgumentNullException>(() => service.TryBuild(default, null!, out _));
        Assert.Throws<ArgumentNullException>(() => service.BuildPreview(null!, PresencePreset.Career, new PresenceTemplates(), out _));
        Assert.Throws<ArgumentNullException>(() => service.BuildPreview(language, PresencePreset.Career, null!, out _));
    }

    private sealed class TestClock : TimeProvider
    {
        public long Timestamp { get; set; }

        public override long TimestampFrequency
            => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
            => Timestamp;
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

        public GameTexts GameTexts
            => GameTexts.English;

        public GameTexts GameLanguageTexts
            => GameTexts.English;

        public LanguageInfo Resolve(string setting, string? gameLanguageCode)
            => LanguageCatalog.Resolve(setting, gameLanguageCode);

        public string? ReadGameLanguageCode(string? profilePath)
            => throw new NotSupportedException();

        public void LoadGameTexts(string? gameDirectory, LanguageInfo language, LanguageInfo gameLanguage)
            => throw new NotSupportedException();

        public string GetOwnText(OwnText text, LanguageInfo language)
            => throw new NotSupportedException();
    }
}
