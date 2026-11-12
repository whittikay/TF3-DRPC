using System;

namespace Tf3Drpc.App.Constants;

/// <summary>
/// Names, intervals and limits of the companion app.
/// </summary>
internal static class AppConstants
{
    public const string ApplicationName = "TF3-DRPC";

    public const string GameProcessName = "TransportFever3";

    public const string GameSteamAppId = "3493540";

    public const string GameFolderName = "Transport Fever 3";

    public const string DataFolderName = "DATA";

    public const string LogFolderName = "logs";

    public const string LogFilePrefix = "TF3-DRPC_";

    public const string LogFileExtension = ".log";

    public const string SettingsFileName = "settings.json";

    public const string WriteProbePrefix = ".write-test-";

    public const string TrayIconFileName = "tray.ico";

#pragma warning disable S1075 // The project's pages are fixed addresses
    public const string RepositoryUrl = "https://github.com/Zagrthos/TF3-DRPC";

    /// <summary>
    /// The mod's page on mod.io; the last part is set at the first upload, so check it then.
    /// </summary>
    public const string ModIoUrl = "https://mod.io/g/transportfever3/m/discord-rich-presence";
#pragma warning restore S1075

    /// <summary>
    /// The button texts in the settings window's footer; names, so they need no translation.
    /// </summary>
    public const string RepositoryButtonText = "GitHub";

    public const string ModIoButtonText = "mod.io";

    public const int LogRetentionDays = 7;

    /// <summary>
    /// The settings window's start size in device-independent pixels; Mac Catalyst needs it in code as well as in XAML.
    /// </summary>
    public const double SettingsWindowWidth = 520;

    public const double SettingsWindowHeight = 780;

    public const double SettingsWindowMinimumWidth = 400;

    public const double SettingsWindowMinimumHeight = 500;

    public const int MaxProfileBytes = 1024 * 1024;

    public static readonly TimeSpan TemplateSaveDelay = TimeSpan.FromSeconds(1);

    public static readonly TimeSpan TranslationRetryInterval = TimeSpan.FromSeconds(30);
}
