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
    public static string RuntimeDirectory { get; private set; }
    public static string ConfigDirectory { get; private set; }
    public static string DefaultConfigDirectory { get; private set; }
    public static ConfigFile TracksConfig { get; private set; }
    public static ConfigFile ExtrasConfig { get; private set; }

    private void Awake()
    {
        Logger = base.Logger;
        RuntimeDirectory = Path.GetDirectoryName(Info.Location);
        ConfigDirectory = Path.Combine(Paths.ConfigPath, NAME);
        DefaultConfigDirectory = Path.Combine(RuntimeDirectory, "defaultconfig");

        TracksConfig = GetConfigFile(GUID + ".Tracks.cfg");
        ExtrasConfig = GetConfigFile(GUID + ".Extras.cfg");

        MusicSwapperSystem.Init();
        TeleporterAudioAdjuster.Init();
        SolutionalHauntMusic.Init();
    }

    private ConfigFile GetConfigFile(string relativePath)
    {
        string configPath = Path.Combine(ConfigDirectory, relativePath);
        if (!File.Exists(configPath))
        {
            string defaultConfigPath = Path.Combine(DefaultConfigDirectory, relativePath);
            if (File.Exists(defaultConfigPath))
            {
                Logger.LogMessage($"Regenerating the config file {relativePath} from the default config file");
                File.Copy(defaultConfigPath, configPath);
            }
        }
        return new ConfigFile(configPath, true, Info.Metadata);
    }
}
