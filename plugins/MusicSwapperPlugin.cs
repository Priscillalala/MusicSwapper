using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HG.Reflection;
using System.Security;
using System.Security.Permissions;
using Path = System.IO.Path;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
[assembly: SearchableAttribute.OptIn]

namespace MusicSwapper;

[BepInPlugin(GUID, NAME, VERSION)]
public class MusicSwapperPlugin : BaseUnityPlugin
{
    public const string
        GUID = "groovesalad." + NAME,
        NAME = "MusicSwapper",
        VERSION = "1.0.0";

    public static new ManualLogSource Logger { get; private set; }
    public static new ConfigFile Config { get; private set; }
    public static string RuntimeDirectory { get; private set; }

    private void Awake()
    {
        Logger = base.Logger;
        Config = base.Config;
        RuntimeDirectory = Path.GetDirectoryName(Info.Location);

        if (!File.Exists(Config.ConfigFilePath))
        {
            string relativeConfigFilePath = Path.GetRelativePath(Paths.ConfigPath, Config.ConfigFilePath);
            string starterConfigPath = Path.Combine(RuntimeDirectory, "defaultconfig", relativeConfigFilePath);
            if (File.Exists(starterConfigPath))
            {
                Logger.LogMessage("Regenerating the config file from the default config file");
                File.Copy(starterConfigPath, Config.ConfigFilePath);
                Config.Reload();
            }
        }

        MusicSwapperSystem.Init();
    }
}
