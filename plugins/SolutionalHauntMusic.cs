using BepInEx.Configuration;
using EntityStates.SolusWing2;
using RoR2;

namespace MusicSwapper;

// Also in the Wolfo Quality of Life mod
public static class SolutionalHauntMusic
{
    public static void Init()
    {
        ConfigEntry<bool> configEntry = MusicSwapperPlugin.ExtrasConfig.Bind(
            "Solutional Haunt",
            "Restart Music After Fight",
            false,
            "After the Solus Wing fight, resume playing the main track of Solutional Haunt.");
        if (configEntry.Value)
        {
            On.EntityStates.SolusWing2.Mission5Death.OnEnter += Mission5Death_OnEnter;
        }
    }

    private static void Mission5Death_OnEnter(On.EntityStates.SolusWing2.Mission5Death.orig_OnEnter orig, Mission5Death self)
    {
        orig(self);
        if (self.solutionalHauntReferences && self.solutionalHauntReferences.PostFightMusic)
        {
            self.solutionalHauntReferences.PostFightMusic.AddComponent<DestroyOnTimer>().duration = 10f;
        }
    }
}
