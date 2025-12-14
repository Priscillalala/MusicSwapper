
using BepInEx.Configuration;
using HG;
using JetBrains.Annotations;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine.AddressableAssets;

namespace MusicSwapper;

public static class MusicSwapperCatalog
{
    public record MusicTrackConfigurationResult(ConfigEntry<TrackTitle> ConfigEntry, ConfigEntry<TrackTitle> PostLoopConfigEntry);
    
    public struct SceneMusicConfigurationResult
    {
        public struct TrackResult
        {
            public ConfigEntry<TrackTitle> configEntry;
            public ConfigEntry<TrackTitle> postLoopConfigEntry;
        }

        public TrackResult mainTrack;
        public TrackResult bossTrack;
    }

    private static HashSet<char> invalidConfigChars = ['=', '\n', '\t', '\\', '"', '\'', '[', ']'];
    private static ExpansionDef dlc1;
    private static MusicTrackDef muNone;
    private static SceneDef[] configValidScenes;

    public static void Init()
    {
        MusicSwapperPlugin.Logger.LogMessage("Music swapper catalog init");

        #region setup assets
        dlc1 = Addressables.LoadAssetAsync<ExpansionDef>(RoR2_DLC1_Common.DLC1_asset).WaitForCompletion();
        muNone = Addressables.LoadAssetAsync<MusicTrackDef>(RoR2_Base_Common_MusicTrackDefs.muNone_asset).WaitForCompletion();

        HashSet<UnityObjectWrapperKey<SceneDef>> infiniteTowerStages = [];
        var sgInfiniteTowerStageXHandle = Addressables.LoadAssetAsync<SceneCollection>(RoR2_DLC1_GameModes_InfiniteTowerRun_SceneGroups.sgInfiniteTowerStageX_asset);
        SceneCollection sgInfiniteTowerStageX = sgInfiniteTowerStageXHandle.WaitForCompletion();
        if (sgInfiniteTowerStageX)
        {
            foreach (var sceneEntry in sgInfiniteTowerStageX.sceneEntries)
            {
                infiniteTowerStages.Add(sceneEntry.sceneDef);
                MusicSwapperPlugin.Logger.LogMessage($"Found simulacrum stage: {sceneEntry.sceneDef.cachedName}");
            }
        }
        #endregion

        configValidScenes = [.. SceneCatalog.allSceneDefs.Where(SceneValidForConfig)];

        #region register music
        HashSet<UnityObjectWrapperKey<MusicTrackDef>> allMusicTracks = [];
        HashSet<UnityObjectWrapperKey<MusicTrackDef>> alwaysAvailableMusicTracks = [];
        Dictionary<UnityObjectWrapperKey<MusicTrackDef>, HashSet<UnityObjectWrapperKey<EntitlementDef>>> entitlementLockedMusicTracks = [];
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
        foreach (SceneDef scene in configValidScenes)
        {
            EntitlementDef sceneRequiredEntitlement = GetRequiredEntitlmentFromScene(scene, infiniteTowerStages);
            if (SceneValidForMainTrack(scene))
            {
                RegisterMusicTrack(scene.mainTrack, sceneRequiredEntitlement);
            }
            if (SceneValidForBossTrack(scene, infiniteTowerStages))
            {
                RegisterMusicTrack(scene.bossTrack, sceneRequiredEntitlement);
            }
        }
        // Ensure that none is always an option
        RegisterMusicTrack(muNone, null);
        #endregion

        TrackNamesCatalog.SetAllTracks(allMusicTracks);

        #region configure music
        var consistentLanguageDictionary = ConsistentLanguageCatalog.BuildConsistentLanguageDictionary();
        ConfigEntry<TrackTitle> CreateMusicTrackConfigEntry(string section, string key, string desc, string specialValuesDesc, string defaultValue, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry)
        {
            List<TrackTitle> acceptableValues = [.. TrackNamesCatalog.AllTrackTitles];
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

            return MusicSwapperPlugin.Config.Bind<TrackTitle>(section, key, defaultValue, new ConfigDescription(desc, new AcceptableValueList<TrackTitle>([.. acceptableValues])));
        }
        ConfigEntry<TrackTitle> ConfigureMusicTrack(ref MusicTrackDef musicTrack, SceneDef scene, string section, string key, string description, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry)
        {
            string defaultValue = musicTrack.cachedName;
            TrackNamesCatalog.InternalNameToAlbumName.TryGetValue(defaultValue, out defaultValue);

            var configEntry = CreateMusicTrackConfigEntry(
                section, 
                key,
                description, 
                string.Empty, 
                defaultValue, 
                parentConfigEntry);
            if (ShouldApplyMusicTrackConfig(configEntry, scene, parentConfigEntry, out MusicTrackDef chosenMusicTrack))
            {
                musicTrack = chosenMusicTrack;
            }
            return configEntry;
        }
        ConfigEntry<TrackTitle> ConfigureMusicTrackPostLoop(PostLoopMusic.RegisterPostLoopTrackDelegate registerPostLoopTrack, SceneDef scene, string section, string key, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry)
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
                registerPostLoopTrack(scene, chosenPostLoopMusicTrack);
            }
            return postLoopConfigEntry;
        }
        bool ShouldApplyMusicTrackConfig(ConfigEntry<TrackTitle> configEntry, SceneDef scene, [CanBeNull] ConfigEntry<TrackTitle> parentConfigEntry, out MusicTrackDef chosenMusicTrack)
        {
            chosenMusicTrack = null;
            if (string.Equals(configEntry.Value, Constants.INHERIT_MUSIC, StringComparison.OrdinalIgnoreCase))
            {
                if (parentConfigEntry != null)
                {
                    return ShouldApplyMusicTrackConfig(parentConfigEntry, scene, null, out chosenMusicTrack);
                }
                return false;
            }
            if (string.Equals(configEntry.Value, (TrackTitle)configEntry.DefaultValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (string.Equals(configEntry.Value, Constants.DEFAULT_MUSIC, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!TrackNamesCatalog.TrackNameToMusicTrackDef.TryGetValue(configEntry.Value, out chosenMusicTrack))
            {
                return false;
            }
            if (entitlementLockedMusicTracks.TryGetValue(chosenMusicTrack, out var entitlementOptions))
            {
                EntitlementDef sceneRequiredEntitlement = GetRequiredEntitlmentFromScene(scene, infiniteTowerStages);
                if (!sceneRequiredEntitlement || !entitlementOptions.Contains(sceneRequiredEntitlement))
                {
                    return false;
                }
            }
            return true;
        }
        SceneMusicConfigurationResult ConfigureSceneMusic(SceneDef scene, SceneMusicConfigurationResult.TrackResult mainTrackParent, SceneMusicConfigurationResult.TrackResult bossTrackParent)
        {
            SceneMusicConfigurationResult result = default;
            string section = GetConfigSafeString(GetConsistentSceneName(scene, consistentLanguageDictionary));
            bool validForMainTrack = SceneValidForMainTrack(scene);
            bool validForBossTrack = SceneValidForBossTrack(scene, infiniteTowerStages);
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
                result.mainTrack.postLoopConfigEntry = ConfigureMusicTrackPostLoop(PostLoopMusic.mainTrackData.RegisterTrack, scene, section, Constants.MAIN_TRACK, mainTrackParent.postLoopConfigEntry);
            }
            if (validForBossTrack)
            {
                result.bossTrack.postLoopConfigEntry = ConfigureMusicTrackPostLoop(PostLoopMusic.bossTrackData.RegisterTrack, scene, section, Constants.BOSS_TRACK, bossTrackParent.postLoopConfigEntry);
            }
            return result;
        }
        foreach (var baseNameSceneGroup in configValidScenes.GroupBy(x => x.baseSceneName))
        {
            SceneMusicConfigurationResult parentResult = default;
            MusicTrackDef parentMainTrack = null, parentBossTrack = null;
            List<SceneDef> scenesToConfigure = new(baseNameSceneGroup);
            if (scenesToConfigure.Count > 1)
            {
                int parentSceneIndex = scenesToConfigure.FindIndex(x => x.cachedName == baseNameSceneGroup.Key);
                if (parentSceneIndex >= 0)
                {
                    SceneDef parentScene = scenesToConfigure[parentSceneIndex];
                    if (parentScene)
                    {
                        MusicSwapperPlugin.Logger.LogMessage($"{parentScene.cachedName} is parent scene");
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

        PostLoopMusic.Init();

        #region cleanup
        ConsistentLanguageCatalog.Cleanup();

        sgInfiniteTowerStageXHandle.Release();

        invalidConfigChars = null;
        #endregion
    }

    // TODO: This breaks if a simulacrum stage requires entitlement from a different DLC
    public static EntitlementDef GetRequiredEntitlmentFromScene(SceneDef scene, HashSet<UnityObjectWrapperKey<SceneDef>> infiniteTowerStages)
    {
        EntitlementDef requiredEntitlment = scene.requiredExpansion ? scene.requiredExpansion.requiredEntitlement : null;
        if (!requiredEntitlment && infiniteTowerStages.Contains(scene) && dlc1)
        {
            requiredEntitlment = dlc1.requiredEntitlement;
        }
        return requiredEntitlment;
    }

    public static string GetConsistentSceneName(SceneDef scene, Dictionary<string, string> consistentLanguageDictionary)
    {
        if (string.IsNullOrEmpty(scene.nameToken) 
            || !consistentLanguageDictionary.TryGetValue(scene.nameToken, out string nameString)
            || string.IsNullOrWhiteSpace(nameString))
        {
            return scene.cachedName;
        }
        return $"{nameString} ({scene.cachedName})";
    }

    public static bool SceneValidForConfig(SceneDef scene)
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

    public static bool SceneValidForMainTrack(SceneDef scene)
    {
        return scene.mainTrack;
    }

    public static bool SceneValidForBossTrack(SceneDef scene, HashSet<UnityObjectWrapperKey<SceneDef>> infiniteTowerStages)
    {
        return scene.bossTrack && scene.hasAnyDestinations && !infiniteTowerStages.Contains(scene);
    }

    public static string GetConfigSafeString(string val)
    {
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
}
