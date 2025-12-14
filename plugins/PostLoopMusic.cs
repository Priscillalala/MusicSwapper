using HG;
using MonoMod.Cil;
using RoR2;

namespace MusicSwapper;

public static class PostLoopMusic
{
    public class PostLoopTrackData(string trackName)
    {
        private readonly string trackName = trackName;
        public Dictionary<UnityObjectWrapperKey<SceneDef>, MusicTrackDef> tracks;
        public MusicTrackDef mostRecentTrack;
        public bool setHook;

        public bool HasTracks => tracks != null && tracks.Count > 0;

        public void RegisterTrack(SceneDef scene, MusicTrackDef postLoopTrack)
        {
            (tracks ??= [])[scene] = postLoopTrack;
            MusicSwapperPlugin.Logger.LogMessage($"Register post loop {trackName} {postLoopTrack.cachedName} for {scene.cachedName}");
        }

        public void Init()
        {
            if (HasTracks)
            {
                SceneCatalog.onMostRecentSceneDefChanged += OnMostRecentSceneDefChanged;
            }
        }

        private void OnMostRecentSceneDefChanged(SceneDef mostRecentSceneDef)
        {
            mostRecentTrack = null;
            if (!Run.instance || Run.instance.loopClearCount <= 0)
            {
                return;
            }
            if (tracks.TryGetValue(mostRecentSceneDef, out mostRecentTrack) && !setHook)
            {
                setHook = true;
                IL.RoR2.MusicController.PickCurrentTrack += OverrideTrackPostLoop;
            }
        }

        private void OverrideTrackPostLoop(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<SceneDef>(trackName)))
            {
                c.EmitDelegate<Func<MusicTrackDef, MusicTrackDef>>(track => mostRecentTrack ? mostRecentTrack : track);
            }
        }
    }

    public delegate void RegisterPostLoopTrackDelegate(SceneDef scene, MusicTrackDef postLoopTrack);

    public static readonly PostLoopTrackData mainTrackData = new(nameof(SceneDef.mainTrack));
    public static readonly PostLoopTrackData bossTrackData = new(nameof(SceneDef.bossTrack));

    public static void Init()
    {
        mainTrackData.Init();
        bossTrackData.Init();
    }
}
