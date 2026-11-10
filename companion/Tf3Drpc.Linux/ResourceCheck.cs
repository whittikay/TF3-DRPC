using System;
using System.Globalization;

using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Languages;
using Tf3Drpc.Core.Resources;

namespace Tf3Drpc.Linux;

/// <summary>
/// Checks that the published executable contains Core's translated resources without starting a desktop or Discord connection.
/// </summary>
internal static class ResourceCheck
{
    internal static int Run()
    {
        string english = OwnTexts.Get(OwnText.StartingTheGame, CultureInfo.GetCultureInfo("en-US"));

        foreach (LanguageInfo language in LanguageCatalog.All)
        {
            string translated = OwnTexts.Get(OwnText.StartingTheGame, language.Culture);

            if (string.IsNullOrWhiteSpace(translated) ||
                string.Equals(translated, nameof(OwnText.StartingTheGame), StringComparison.Ordinal) ||
                (language.Code is not "en" && string.Equals(translated, english, StringComparison.Ordinal)))
            {
                Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Missing translated resource: {language.Code}"));

                return 1;
            }
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Resource check passed for {LanguageCatalog.All.Count} languages."));

        return 0;
    }
}
