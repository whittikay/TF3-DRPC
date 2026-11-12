using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Monitor;
using Tf3Drpc.Core.Settings;

namespace Tf3Drpc.Application.Services.Interfaces;

/// <summary>
/// The app's settings.
/// </summary>
public interface ISettingsService : ISettingsSource
{
    /// <summary>
    /// Tells whether the data folder was writable when last probed; a save probes it again when it wasn't.
    /// </summary>
    bool CanSave { get; }

    /// <summary>
    /// Tells whether the current settings could not be saved the last time; changing any setting again retries.
    /// </summary>
    bool LastSaveFailed { get; }

    /// <summary>
    /// Changes the language setting (<c>game</c> or a language code) and saves it, also when it is unchanged but the last save failed; unknown values are ignored.
    /// </summary>
    bool SetLanguage(string language);

    /// <summary>
    /// Changes the preset of the presence and saves it, also when it is unchanged but the last save failed.
    /// </summary>
    bool SetPreset(PresencePreset preset);

    /// <summary>
    /// Changes the theme of the settings window and saves it, also when it is unchanged but the last save failed.
    /// </summary>
    bool SetTheme(ThemeMode theme);

    /// <summary>
    /// Changes the templates of the custom preset and saves them, also when they are unchanged but the last save failed; the templates are normalised first.
    /// </summary>
    bool SetTemplates(PresenceTemplates templates);

    /// <summary>
    /// Switches the store button of the presence on or off and saves it, also when it is unchanged but the last save failed.
    /// </summary>
    bool SetStoreButton(bool enabled);
}
