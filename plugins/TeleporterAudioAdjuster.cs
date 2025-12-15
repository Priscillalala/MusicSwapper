using BepInEx.Configuration;
using RoR2;
using UnityEngine;

namespace MusicSwapper;

public static class TeleporterAudioAdjuster
{
    private static float proximityScalar;

    public static void Init()
    {
        const float DEFAULT_VALUE = 1f;
        ConfigEntry<float> volumeMultiplierConfigEntry = MusicSwapperPlugin.ExtrasConfig.Bind(
            "Teleporter Hum",
            "Volume Multiplier",
            DEFAULT_VALUE,
            new ConfigDescription("Reduce this value to dampen the volume of Teleporter hum (the Teleporter hum emits from the Teleporter and plays along with a stage's main track).", new AcceptableValueRange<float>(0f, 1f)));
        if (volumeMultiplierConfigEntry.Value != DEFAULT_VALUE)
        {
            MusicSwapperPlugin.Logger.LogMessage($"{nameof(TeleporterAudioAdjuster)} extra is active");
            proximityScalar = Mathf.Clamp01(volumeMultiplierConfigEntry.Value);
            On.RoR2.MusicController.UpdateTeleporterParameters += MusicController_UpdateTeleporterParameters;
        }
    }

    private static void MusicController_UpdateTeleporterParameters(On.RoR2.MusicController.orig_UpdateTeleporterParameters orig, MusicController self, TeleporterInteraction teleporter, Transform cameraTransform, CharacterBody targetBody)
    {
        orig(self, teleporter, cameraTransform, targetBody);
        const float VALUE_MAX = 10000f;
        float value = self.rtpcTeleporterProximityValue.value;
        self.rtpcTeleporterProximityValue.value = Util.Remap(value, 0f, VALUE_MAX, VALUE_MAX - (VALUE_MAX * proximityScalar), VALUE_MAX);
    }
}
