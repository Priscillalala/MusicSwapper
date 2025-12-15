using BepInEx.Configuration;
using HG;
using JetBrains.Annotations;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine.AddressableAssets;
using static RoR2.SceneCollection;

namespace MusicSwapper;

public static class MusicSwapperSystem
{
    private struct SceneMusicConfigurationResult
    {
        public struct TrackResult
        {
            public ConfigEntry<TrackTitle> configEntry;
            public ConfigEntry<TrackTitle> postLoopConfigEntry;
        }

        public TrackResult mainTrack;
        public TrackResult bossTrack;
    }

    private static TrackTitlesInstance trackTitles;
    private static ConsistentLanguageInstance consistentLanguage;

    public static void Init()
    {
        trackTitles = new("TrackTitles.json");
        consistentLanguage = new();
        RoR2Application.onLoad += OnLoad;
    }

    private static void OnLoad()
    {
        DoConfiguration();

        PostLoopMusicManager.Init();

        trackTitles = null;
        consistentLanguage.Dispose();
        consistentLanguage = null;
    }

    private static void DoConfiguration()
    {
        ExpansionDef dlc1 = Addressables.LoadAssetAsync<ExpansionDef>(RoR2_DLC1_Common.DLC1_asset).WaitForCompletion();

        HashSet<UnityObjectWrapperKey<SceneDef>> infiniteTowerStages = [];
        var sgInfiniteTowerStageXHandle = Addressables.LoadAssetAsync<SceneCollection>(RoR2_DLC1_GameModes_InfiniteTowerRun_SceneGroups.sgInfiniteTowerStageX_asset);
        SceneCollection sgInfiniteTowerStageX = sgInfiniteTowerStageXHandle.WaitForCompletion();
        if (sgInfiniteTowerStageX)
        {
            foreach (var sceneEntry in sgInfiniteTowerStageX.sceneEntries)
            {
                infiniteTowerStages.Add(sceneEntry.sceneDef);
            }
        }
        else
        {
            MusicSwapperPlugin.Logger.LogError($"Failed to load sgInfiniteTowerStageX!");
        }

        SceneDef[] configValidScenes = [.. SceneCatalog.allSceneDefs.Where(SceneValidForConfig)];

        #region register music
        HashSet<UnityObjectWrapperKey<MusicTrackDef>> allMusicTracks = [];
        HashSet<UnityObjectWrapperKey<MusicTrackDef>> alwaysAvailableMusicTracks = [];
        Dictionary<UnityObjectWrapperKey<MusicTrackDef>, HashSet<UnityObjectWrapperKey<EntitlementDef>>> entitlementLockedMusicTracks = [];

        foreach (SceneDef scene in configValidScenes)
        {
            EntitlementDef sceneRequiredEntitlement = GetRequiredEntitlementFromScene(scene);
            if (SceneValidForMainTrack(scene))
            {
                RegisterMusicTrack(scene.mainTrack, sceneRequiredEntitlement);
            }
            if (SceneValidForBossTrack(scene))
            {
                RegisterMusicTrack(scene.bossTrack, sceneRequiredEntitlement);
            }
        }

        // Ensure that none is always an option
        MusicTrackDef muNone = Addressables.LoadAssetAsync<MusicTrackDef>(RoR2_Base_Common_MusicTrackDefs.muNone_asset).WaitForCompletion();
        if (muNone)
        {
            RegisterMusicTrack(muNone, null);
        }
        else
        {
            MusicSwapperPlugin.Logger.LogError($"Failed to load muNone!");
        }
        #endregion

        trackTitles.SetAllTracks(allMusicTracks);

        #region configure music
        var consistentLanguageDictionary = consistentLanguage.BuildConsistentLanguageDictionary();
        HashSet<char> invalidConfigChars = ['=', '\n', '\t', '\\', '"', '\'', '[', ']']; // Copied from BepInEx.Configuration.ConfigDefinition

        foreach (var baseNameSceneGroup in configValidScenes.GroupBy(x => x.baseSceneName))
        {
            SceneMusicConfigurationResult parentResult = default;
            MusicTrackDef parentMainTrack = null, parentBossTrack = null;
            List<SceneDef> scenesToConfigure = [.. baseNameSceneGroup];
            if (scenesToConfigure.Count > 1)
            {
                int parentSceneIndex = scenesToConfigure.FindIndex(x => x.cachedName == baseNameSceneGroup.Key);
                if (parentSceneIndex >= 0)
                {
                    SceneDef parentScene = scenesToConfigure[parentSceneIndex];
                    if (parentScene)
                    {
                        parentMainTrack = parentScene.mainTrack;
                        parentBossTrack = parentScene.bossTrack;
                        parentResult = ConfigureSceneMusic(parentScene, default, default);
                    }
                    scenesToConfigure.RemoveAt(parentSceneIndex);
                }
            }
            foreach (SceneDef scene in scenesToConfigure)
            {
                ConfigureSceneMusic(
                    scene,
                    scene.mainTrack == parentMainTrack ? parentResult.mainTrack : default,
                    scene.bossTrack == parentBossTrack ? parentResult.bossTrack : default);
            }
        }
        #endregion

        sgInfiniteTowerStageXHandle.Release();

        invalidConfigChars = null;

        void RegisterMusicTrack(MusicTrackDef musicTrack, [CanBeNull] EntitlementDef requiredEntitlement)
        {
            allMusicTracks.Add(musicTrack);
            if (alwaysAvailableMusicTracks.Contains(musicTrack))
            {
                return;
            }
            if (!requiredEntitlement || EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(requiredEntitlement))
            {
                alwaysAvailableMusicTracks.Add(musicTrack);
                entitlementLockedMusicTracks.Remove(musicTrack);
                return;
            }
            if (entitlementLockedMusicTracks.TryGetValue(musicTrack, out var entitlementOptions))
            {
                entitlementOptions.Add(requiredEntitlement);
                return;
            }
            entitlementLockedMusicTracks.Add(musicTrack, [requiredEntitlement]);
        }

        SceneMusicConfigurationResult ConfigureSceneMusic(SceneDef scene, SceneMusicConfigurationResult.TrackResult mainTrackParent, SceneMusicConfigurationResult.TrackResult bossTrackParent)
        {
            SceneMusicConfigurationResult result = default;
            string section = GetConfigSectionForScene(scene, consistentLanguageDictionary);
            bool validForMainTrack = SceneValidForMainTrack(scene);
            bool validForBossTrack = SceneValidForBossTrack(scene);
            if (validForMainTrack)
            {
                result.mainTrack.configEntry = ConfigureMusicTrack(ref scene.mainTrack, scene, section, Constants.MAIN_TRACK, "Choose a track for the stage.", mainTrackParent.configEntry);
            }
            if (validForBossTrack)
            {
                result.bossTrack.configEntry = ConfigureMusicTrack(ref scene.bossTrack, scene, section, Constants.BOSS_TRACK, "Choose a track for the Teleporter event.", bossTrackParent.configEntry);
            }
            if (validForMainTrack)
            {
                result.mainTrack.postLoopConfigEntry = ConfigureMusicTrackPostLoop(PostLoopMusicManager.mainTrackData, scene, section, Constants.MAIN_TRACK, mainTrackParent.postLoopConfigEntry);
            }
            if (validForBossTrack)
            {
                result.bossTrack.postLoopConfigEntry = ConfigureMusicTrackPostLoop(PostLoopMusicManager.bossTrackData, scene, section, Constants.BOSS_TRACK, bossTrackParent.postLoopConfigEntry);
            }
            return result;
        }

        ConfigEntry<TrackTitle> ConfigureMusicTrack(ref MusicTrackDef musicTrack, SceneDef scene, string section, string key, string description, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry)
        {
            string defaultValue = musicTrack.cachedName;
            trackTitles.InternalNameToAlbumName.TryGetValue(defaultValue, out defaultValue);

            var configEntry = CreateMusicTrackConfigEntry(
                section,
                key,
                description,
                string.Empty,
                defaultValue,
                parentConfigEntry);
            if (ShouldApplyMusicTrackConfig(configEntry, scene, parentConfigEntry, out MusicTrackDef chosenMusicTrack))
            {
                MusicSwapperPlugin.Logger.LogMessage($"Setting the {scene.cachedName} {key} to {chosenMusicTrack.cachedName} (formerly {musicTrack.cachedName})");
                musicTrack = chosenMusicTrack;
            }
            return configEntry;
        }

        ConfigEntry<TrackTitle> ConfigureMusicTrackPostLoop(PostLoopMusicManager.IPostLoopTrackData postLoopTrackData, SceneDef scene, string section, string key, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry)
        {
            var postLoopConfigEntry = CreateMusicTrackConfigEntry(
                section,
                $"Post-Loop {key}",
                $"Choose a track to override the {key} after looping once.",
                $"\nChoose \"{Constants.DEFAULT_MUSIC}\" to disable this feature.",
                Constants.DEFAULT_MUSIC,
                parentConfigEntry);
            if (ShouldApplyMusicTrackConfig(postLoopConfigEntry, scene, parentConfigEntry, out MusicTrackDef chosenPostLoopMusicTrack))
            {
                MusicSwapperPlugin.Logger.LogMessage($"Setting the {scene.cachedName} {postLoopConfigEntry.Definition.Key} to {chosenPostLoopMusicTrack.cachedName}");
                postLoopTrackData.RegisterTrack(scene, chosenPostLoopMusicTrack);
            }
            return postLoopConfigEntry;
        }

        ConfigEntry<TrackTitle> CreateMusicTrackConfigEntry(string section, string key, string desc, string specialValuesDesc, string defaultValue, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry)
        {
            List<TrackTitle> acceptableValues = [.. trackTitles.PrimaryTrackTitles];
            if (parentConfigEntry != null)
            {
                if (!acceptableValues.Contains(defaultValue))
                {
                    acceptableValues.Insert(0, defaultValue);
                }
                defaultValue = Constants.INHERIT_MUSIC;
                specialValuesDesc = $"\nChoose \"{Constants.INHERIT_MUSIC}\" to use the same {key} as {parentConfigEntry.Definition.Section}." + specialValuesDesc;
            }
            acceptableValues.Remove(defaultValue);
            acceptableValues.Insert(0, defaultValue);

            desc += " Some tracks require DLC.";
            if (!string.IsNullOrEmpty(specialValuesDesc))
            {
                desc += "\nSpecial Values:" + specialValuesDesc;
            }

            return MusicSwapperPlugin.Config.Bind<TrackTitle>(section, key, defaultValue, new ConfigDescription(desc, new HiddenAcceptableValueList<TrackTitle>([.. acceptableValues], trackTitles.AllTrackTitles)));
        }

        bool ShouldApplyMusicTrackConfig(ConfigEntry<TrackTitle> configEntry, SceneDef scene, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry, out MusicTrackDef chosenMusicTrack)
        {
            chosenMusicTrack = null;
            if (string.Equals(configEntry.Value, Constants.INHERIT_MUSIC))
            {
                if (parentConfigEntry != null)
                {
                    return ShouldApplyMusicTrackConfig(parentConfigEntry, scene, null, out chosenMusicTrack);
                }
                return false;
            }
            if (string.Equals(configEntry.Value, (TrackTitle)configEntry.DefaultValue))
            {
                return false;
            }
            if (string.Equals(configEntry.Value, Constants.DEFAULT_MUSIC))
            {
                return false;
            }
            if (!trackTitles.TrackNameToMusicTrackDef.TryGetValue(configEntry.Value, out chosenMusicTrack))
            {
                return false;
            }
            if (entitlementLockedMusicTracks.TryGetValue(chosenMusicTrack, out var entitlementOptions))
            {
                EntitlementDef sceneImplicitEntitlement = GetRequiredEntitlementFromScene(scene);
                if (!sceneImplicitEntitlement || !entitlementOptions.Contains(sceneImplicitEntitlement))
                {
                    MusicSwapperPlugin.Logger.LogWarning($"The {scene.cachedName} {configEntry.Definition.Key} cannot be set to {chosenMusicTrack.cachedName} because the track's entitlement requirements are not met");
                    return false;
                }
            }
            return true;
        }

        string GetConfigSectionForScene(SceneDef scene, Dictionary<string, string> consistentLanguageDictionary)
        {
            string section;
            if (!string.IsNullOrEmpty(scene.nameToken)
                && consistentLanguageDictionary.TryGetValue(scene.nameToken, out string nameString)
                && !string.IsNullOrWhiteSpace(nameString))
            {
                section = $"{nameString} ({scene.cachedName})";
            }
            else
            {
                section = scene.cachedName;
            }
            return GetConfigSafeString(section);
        }

        string GetConfigSafeString(string val)
        {
            // Abuse unicode so stages like Siren's Call appear properly
            val = val.Replace('\'', 'ꞌ');
            for (int i = val.Length - 1; i >= 0; i--)
            {
                if (invalidConfigChars.Contains(val[i]))
                {
                    val = val.Remove(i, 1);
                }
            }
            return val;
        }

        // TODO: This breaks if a simulacrum stage requires entitlement from a different DLC
        EntitlementDef GetRequiredEntitlementFromScene(SceneDef scene)
        {
            EntitlementDef requiredEntitlement = scene.requiredExpansion ? scene.requiredExpansion.requiredEntitlement : null;
            if (!requiredEntitlement && infiniteTowerStages.Contains(scene) && dlc1)
            {
                requiredEntitlement = dlc1.requiredEntitlement;
            }
            return requiredEntitlement;
        }

        static bool SceneValidForConfig(SceneDef scene)
        {
            if (scene.isOfflineScene)
            {
                return false;
            }
            SceneType sceneType = scene.sceneType;
            if (sceneType != SceneType.Stage
                && sceneType != SceneType.Intermission
                && sceneType != SceneType.TimedIntermission
                && sceneType != SceneType.UntimedStage)
            {
                return false;
            }
            return true;
        }

        static bool SceneValidForMainTrack(SceneDef scene)
        {
            return scene.mainTrack;
        }

        bool SceneValidForBossTrack(SceneDef scene)
        {
            return scene.bossTrack && scene.hasAnyDestinations && !infiniteTowerStages.Contains(scene);
        }
    }
}