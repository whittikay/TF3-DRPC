using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Monitor;
using Tf3Drpc.Core.Settings;
using Tf3Drpc.Core.Structs;

namespace Tf3Drpc.Application.Services.Interfaces;

/// <summary>
/// Builds the Discord presence from the game state.
/// </summary>
public interface IPresenceService : IPresenceBuilder
{
    /// <summary>
    /// Builds a preview of the presence while playing, from the last status seen or from example values.
    /// </summary>
    /// <param name="language">The language of the texts.</param>
    /// <param name="preset">The preset to preview.</param>
    /// <param name="templates">The templates of the custom preset.</param>
    /// <param name="usesExample">Tells whether example values were used because no status was seen yet.</param>
    PresenceContentStruct BuildPreview(LanguageInfo language, PresencePreset preset, PresenceTemplates templates, out bool usesExample);
}
