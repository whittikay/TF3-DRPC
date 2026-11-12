namespace Tf3Drpc.Application.Services.Interfaces;

/// <summary>
/// Provides the portable settings path and its writable state.
/// </summary>
public interface ISettingsPaths
{
    /// <summary>
    /// The path of the settings file.
    /// </summary>
    string SettingsFilePath { get; }

    /// <summary>
    /// Tells whether the data folder exists and can be written to.
    /// </summary>
    bool IsDataWritable { get; }

    /// <summary>
    /// Probes the data folder again and returns whether it can be written to now.
    /// </summary>
    bool RefreshDataWritable();
}
