using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HG.Reflection;
using RoR2;
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
        Logger.LogMessage("Your GOAT's still got it");

        if (!File.Exists(Config.ConfigFilePath))
        {
            Logger.LogWarning("config not here");
            string relativeConfigFilePath = Path.GetRelativePath(Paths.ConfigPath, Config.ConfigFilePath);
            string starterConfigPath = Path.Combine(RuntimeDirectory, "starterconfig", relativeConfigFilePath);
            if (File.Exists(starterConfigPath))
            {
                Logger.LogWarning("foudn starter config");
                File.Copy(starterConfigPath, Config.ConfigFilePath);
                Config.Reload();
            }
        }

        MusicSwapperSystem.Init();
    }
}
