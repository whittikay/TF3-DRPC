using System;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.Logging;

using Tf3Drpc.Application.Logging;
using Tf3Drpc.Application.Messages;
using Tf3Drpc.Application.Services.Interfaces;
using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Presence;
using Tf3Drpc.Core.Settings;

namespace Tf3Drpc.Application.Services;

/// <summary>
/// Keeps the settings in memory and saves them to <c>DATA/settings.json</c> when they change.
/// </summary>
public sealed class SettingsService(ISettingsPaths paths, IMessenger messenger, ILogger<SettingsService> logger) : ISettingsService
{
    private readonly ISettingsPaths _paths = paths;
    private readonly IMessenger _messenger = messenger;
    private readonly ILogger<SettingsService> _logger = logger;

    public AppSettings Current { get; private set; } = SettingsStore.Load(paths.SettingsFilePath);

    public bool CanSave
        => _paths.IsDataWritable;

    public bool LastSaveFailed { get; private set; }

    public bool SetLanguage(string language)
    {
        if (!LanguageCatalog.IsValidSetting(language))
            return false;

        bool changed = !string.Equals(Current.Language, language, StringComparison.Ordinal);

        if (!changed && !LastSaveFailed)
            return true;

        if (changed)
        {
            Current = Current with { Language = language };
            _logger.LanguageSettingChanged(language);
        }

        return Save();
    }

    public bool SetPreset(PresencePreset preset)
    {
        string setting = PresetCatalog.ToSetting(preset);

        if (!PresetCatalog.TryParse(setting, out _))
            return false;

        bool changed = !string.Equals(Current.Preset, setting, StringComparison.Ordinal);

        if (!changed && !LastSaveFailed)
            return true;

        if (changed)
        {
            Current = Current with { Preset = setting };
            _logger.PresetSettingChanged(setting);
        }

        return Save();
    }

    public bool SetTheme(ThemeMode theme)
    {
        string setting = ThemeCatalog.ToSetting(theme);

        if (!ThemeCatalog.TryParse(setting, out _))
            return false;

        bool changed = !string.Equals(Current.Theme, setting, StringComparison.Ordinal);

        if (!changed && !LastSaveFailed)
            return true;

        if (changed)
        {
            Current = Current with { Theme = setting };
            _logger.ThemeSettingChanged(setting);
        }

        return Save();
    }

    public bool SetTemplates(PresenceTemplates templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        PresenceTemplates normalised = SettingsStore.NormalizeTemplates(templates);
        bool changed = !normalised.Equals(Current.Templates);

        if (!changed && !LastSaveFailed)
            return true;

        if (changed)
        {
            Current = Current with { Templates = normalised };
            _logger.TemplatesSettingChanged();
        }

        return Save();
    }

    public bool SetStoreButton(bool enabled)
    {
        bool changed = Current.StoreButton != enabled;

        if (!changed && !LastSaveFailed)
            return true;

        if (changed)
        {
            Current = Current with { StoreButton = enabled };
            _logger.StoreButtonSettingChanged(enabled);
        }

        return Save();
    }

    private bool Save()
    {
        // A repaired data folder is noticed here; a save that was not written counts as failed.
        bool saved = (CanSave || _paths.RefreshDataWritable()) && SettingsStore.TrySave(_paths.SettingsFilePath, Current);
        LastSaveFailed = !saved;

        if (!saved)
            _logger.SettingsNotSaved();

        _messenger.Send(new SettingsChangedMessage(Current));

        return saved;
    }
}
