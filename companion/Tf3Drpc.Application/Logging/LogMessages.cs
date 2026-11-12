using System;

using Microsoft.Extensions.Logging;

using Tf3Drpc.Application.Services;

namespace Tf3Drpc.Application.Logging;

internal static partial class LogMessages
{
    #region Information

    [LoggerMessage(LogLevel.Information, "Language setting changed to {Language}.")]
    public static partial void LanguageSettingChanged(this ILogger<SettingsService> logger, string language);

    [LoggerMessage(LogLevel.Information, "Preset setting changed to {Preset}.")]
    public static partial void PresetSettingChanged(this ILogger<SettingsService> logger, string preset);

    [LoggerMessage(LogLevel.Information, "Theme setting changed to {Theme}.")]
    public static partial void ThemeSettingChanged(this ILogger<SettingsService> logger, string theme);

    [LoggerMessage(LogLevel.Information, "Store button setting changed to {Enabled}.")]
    public static partial void StoreButtonSettingChanged(this ILogger<SettingsService> logger, bool enabled);

    [LoggerMessage(LogLevel.Information, "Template settings changed.")]
    public static partial void TemplatesSettingChanged(this ILogger<SettingsService> logger);

    #endregion Information

    #region Warning

    [LoggerMessage(LogLevel.Warning, "The settings could not be saved.")]
    public static partial void SettingsNotSaved(this ILogger<SettingsService> logger);

    #endregion Warning

    #region Error

    [LoggerMessage(LogLevel.Error, "The status change could not be sent.")]
    public static partial void StatusSendFailed(this ILogger<MonitorService> logger, Exception exception);

    #endregion Error
}
