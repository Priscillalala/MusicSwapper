using RoR2;
using System.Collections;

namespace MusicSwapper;

// Consistent language used to generate config entries
// Always english and not affected by language overrides
public static class ConsistentLanguageCatalog
{
    private static List<KeyValuePair<string, string>> consistentTokenStringPairs;

    public static void Init()
    {
        On.RoR2.Language.LoadAllTokensFromFolders += Language_LoadAllTokensFromFolders;
    }

    private static void Language_LoadAllTokensFromFolders(On.RoR2.Language.orig_LoadAllTokensFromFolders orig, IEnumerable<string> folders, List<KeyValuePair<string, string>> output)
    {
        orig(folders, output);
        MusicSwapperPlugin.Logger.LogMessage("Language_LoadAllTokensFromFolders");
        Language english = Language.FindLanguageByName("en");
        if (english != null && folders == english.folders)
        {
            MusicSwapperPlugin.Logger.LogMessage("foudn english");
            consistentTokenStringPairs = output;
        }
    }

    public static Dictionary<string, string> BuildConsistentLanguageDictionary()
    {
        Dictionary<string, string> consistentLanguageDictionary = [];
        if (consistentTokenStringPairs == null)
        {
            MusicSwapperPlugin.Logger.LogWarning("Failed to cache consistentTokenStringPairs - generating them from the language folders!");
            if (Language.english == null)
            {
                return consistentLanguageDictionary;
            }
            consistentTokenStringPairs = [];
            Language.LoadAllTokensFromFolders(Language.english.folders, consistentTokenStringPairs);
        }
        foreach (var tokenStringPair in consistentTokenStringPairs)
        {
            // The first tokenStringPair takes precident rather than the last, to ignore language overrides
            consistentLanguageDictionary.TryAdd(tokenStringPair.Key, tokenStringPair.Value);
        }
        return consistentLanguageDictionary;
    }

    public static void Cleanup()
    {
        On.RoR2.Language.LoadAllTokensFromFolders -= Language_LoadAllTokensFromFolders;
        consistentTokenStringPairs = null;
    }
}
