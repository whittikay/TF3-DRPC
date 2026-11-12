using System;

using Microsoft.Extensions.Logging;

using Tf3Drpc.App.Services;
using Tf3Drpc.App.ViewModels;

namespace Tf3Drpc.App.Logging;

internal static partial class LogMessages
{
    #region Information

    [LoggerMessage(LogLevel.Information, "TF3-DRPC {Version} started. Data folder: {DataDirectory}.")]
    public static partial void AppStarted(this ILogger<CompanionApplication> logger, string version, string dataDirectory);

    [LoggerMessage(LogLevel.Information, "TF3-DRPC is exiting.")]
    public static partial void AppExiting(this ILogger<AppLifecycleService> logger);

    [LoggerMessage(LogLevel.Information, "Game texts for {Language} loaded from {Path}.")]
    public static partial void GameTextsLoaded(this ILogger<TranslationService> logger, string language, string path);

    [LoggerMessage(LogLevel.Information, "Tray icon created: {Created}.")]
    public static partial void TrayCreated(this ILogger<PlatformTrayService> logger, bool created);

    #endregion Information

    #region Warning

    [LoggerMessage(LogLevel.Warning, "macOS started the app from a hidden read-only copy (App Translocation); the data folder {DataDirectory} cannot be written. Move the app once with Finder.")]
    public static partial void AppTranslocated(this ILogger<CompanionApplication> logger, string dataDirectory);

    [LoggerMessage(LogLevel.Warning, "The data folder {DataDirectory} is not writable; settings are not saved and no log file is written.")]
    public static partial void DataNotWritable(this ILogger<CompanionApplication> logger, string dataDirectory);

    [LoggerMessage(LogLevel.Warning, "The log folder {LogDirectory} is not writable; no log file is written.")]
    public static partial void LogDirectoryNotWritable(this ILogger<CompanionApplication> logger, string logDirectory);

    [LoggerMessage(LogLevel.Warning, "No usable game texts for {Language} (looked in {GameDirectory}); using English.")]
    public static partial void GameTextsNotFound(this ILogger<TranslationService> logger, string language, string? gameDirectory);

    [LoggerMessage(LogLevel.Warning, "The link {Url} could not be opened.")]
    public static partial void UrlNotOpened(this ILogger<SettingsViewModel> logger, string url);

    #endregion Warning

    #region Error

    [LoggerMessage(LogLevel.Error, "The tray icon could not be created.")]
    public static partial void TrayFailed(this ILogger<PlatformTrayService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "The link {Url} could not be opened.")]
    public static partial void UrlOpenFailed(this ILogger<SettingsViewModel> logger, string url, Exception exception);

    #endregion Error
}
