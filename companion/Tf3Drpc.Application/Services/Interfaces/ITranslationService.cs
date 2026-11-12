using System.Collections.Generic;

using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Monitor;
using Tf3Drpc.Core.Translation;

namespace Tf3Drpc.Application.Services.Interfaces;

/// <summary>
/// The game's texts, the app's own texts and the languages.
/// </summary>
public interface ITranslationService : ILanguageResolver
{
    /// <summary>
    /// The 13 languages.
    /// </summary>
    IReadOnlyList<LanguageInfo> Languages { get; }

    /// <summary>
    /// The game's texts for the chosen language, as last loaded.
    /// </summary>
    GameTexts GameTexts { get; }

    /// <summary>
    /// The game's texts in the game's own language, as last loaded; the same instance as <see cref="GameTexts"/> when both languages are the same.
    /// </summary>
    GameTexts GameLanguageTexts { get; }

    /// <summary>
    /// Returns an own text in a language.
    /// </summary>
    string GetOwnText(OwnText text, LanguageInfo language);
}
