using System;

using Tf3Drpc.Application.Services.Interfaces;
using Tf3Drpc.Core.Constants;
using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Presence;
using Tf3Drpc.Core.Settings;
using Tf3Drpc.Core.Structs;

namespace Tf3Drpc.Application.Services;

/// <summary>
/// Builds the presence and its preview with the game's current texts and the settings (preset, templates, store button).
/// </summary>
public sealed class PresenceService(ITranslationService translation, ISettingsService settings, TimeProvider timeProvider) : IPresenceService
{
    private readonly ITranslationService _translation = translation;
    private readonly ISettingsService _settings = settings;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly long _startTimestamp = timeProvider.GetTimestamp();
    private readonly PresencePreview _preview = new();

    public bool TryBuild(in GameStateSnapshotStruct snapshot, LanguageInfo language, out PresenceContentStruct content)
    {
        ArgumentNullException.ThrowIfNull(language);

        AppSettings current = _settings.Current;
        PresencePreset preset = (PresetCatalog.TryParse(current.Preset, out PresencePreset parsed)) ? parsed : PresencePreset.Career;
        PresenceOptionsStruct options = new()
        {
            Preset = preset,
            RotationStep = GetRotationStep(),
            Templates = current.Templates,
            ShowStoreButton = current.StoreButton,
            GameLanguageTexts = _translation.GameLanguageTexts
        };

        _preview.Remember(snapshot);

        return PresenceBuilder.TryBuild(snapshot, language.Culture, _translation.GameTexts, options, out content);
    }

    public PresenceContentStruct BuildPreview(LanguageInfo language, PresencePreset preset, PresenceTemplates templates, out bool usesExample)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(templates);

        PresenceOptionsStruct options = new()
        {
            Preset = preset,
            RotationStep = GetRotationStep(),
            Templates = templates,
            ShowStoreButton = _settings.Current.StoreButton,
            GameLanguageTexts = _translation.GameLanguageTexts
        };

        return _preview.Build(language.Culture, _translation.GameTexts, options, out usesExample);
    }

    private long GetRotationStep()
        => _timeProvider.GetElapsedTime(_startTimestamp).Ticks / PresenceConstants.AlternatingInterval.Ticks;
}
