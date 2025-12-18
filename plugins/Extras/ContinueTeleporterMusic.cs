using BepInEx.Configuration;
using RoR2;
using RoR2.WwiseUtils;
using UnityEngine;

namespace MusicSwapper.Extras;

public static class ContinueTeleporterMusic
{
    public static void Init()
    {
        ConfigEntry<bool> configEntry = MusicSwapperPlugin.ExtrasConfig.Bind(
            "Teleporter Music",
            "Continue After Charged",
            false,
            "After charging the Teleporter, continue playing the Teleporter track until you leave the stage.");
        if (configEntry.Value)
        {
            MusicSwapperPlugin.Logger.LogMessage($"{nameof(ContinueTeleporterMusic)} extra is active");
            On.RoR2.MusicController.UpdateTeleporterParameters += MusicController_UpdateTeleporterParameters;
        }
    }

    private static void MusicController_UpdateTeleporterParameters(On.RoR2.MusicController.orig_UpdateTeleporterParameters orig, MusicController self, TeleporterInteraction teleporter, Transform cameraTransform, CharacterBody targetBody)
    {
        orig(self, teleporter, cameraTransform, targetBody);
        // inSceneTransition handles portals
        // isInFinalSequence is probably redundant
        bool stopBossMusic = (teleporter && teleporter.isInFinalSequence) || (Run.instance && Run.instance.inSceneTransition);
        self.stBossStatus.valueId = stopBossMusic ? CommonWwiseIds.dead : CommonWwiseIds.alive;
    }
}
