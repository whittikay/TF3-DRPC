using Tf3Drpc.Core.Settings;

namespace Tf3Drpc.Application.Messages;

/// <summary>
/// Message sent when a setting was changed.
/// </summary>
/// <param name="Settings">The settings after the change.</param>
public sealed record class SettingsChangedMessage(AppSettings Settings);
