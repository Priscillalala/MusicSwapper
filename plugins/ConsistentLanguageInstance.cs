using RoR2;

namespace MusicSwapper;

// Consistent language used to generate config entries
// Always english and not affected by language overrides
public class ConsistentLanguageInstance : IDisposable
{
    private List<KeyValuePair<string, string>> consistentTokenStringPairs;

    public ConsistentLanguageInstance()
    {
        On.RoR2.Language.LoadAllTokensFromFolders += Language_LoadAllTokensFromFolders;
    }

    private void Language_LoadAllTokensFromFolders(On.RoR2.Language.orig_LoadAllTokensFromFolders orig, IEnumerable<string> folders, List<KeyValuePair<string, string>> output)
    {
        orig(folders, output);
        // Language.english is not populated yet
        Language english = Language.FindLanguageByName("en");
        if (english != null && folders == english.folders)
        {
            consistentTokenStringPairs = output;
        }
    }

    public Dictionary<string, string> BuildConsistentLanguageDictionary()
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
            // The first tokenStringPair takes priority rather than the last, to ignore language overrides
            consistentLanguageDictionary.TryAdd(tokenStringPair.Key, tokenStringPair.Value);
        }
        return consistentLanguageDictionary;
    }

    public void Dispose()
    {
        On.RoR2.Language.LoadAllTokensFromFolders -= Language_LoadAllTokensFromFolders;
    }
}
