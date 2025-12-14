using HG;
using RoR2;
using SimpleJSON;
using Path = System.IO.Path;

namespace MusicSwapper;

public static class TrackNamesCatalog
{
    public static Dictionary<string, string> InternalNameToAlbumName { get; private set; }
    //public static Dictionary<string, string> AlbumNameToInternalName { get; private set; }
    public static List<TrackTitle> AllTrackTitles { get; private set; }
    public static Dictionary<string, MusicTrackDef> TrackNameToMusicTrackDef { get; private set; }
    //public static Dictionary<UnityObjectWrapperKey<MusicTrackDef>, string> MusicTrackDefToTrackName { get; private set; }

    public static void Init()
    {
        InternalNameToAlbumName = [];
        //AlbumNameToInternalName = new(StringComparer.OrdinalIgnoreCase);
        var json = JSON.Parse(File.ReadAllText(Path.Combine(MusicSwapperPlugin.RuntimeDirectory, "TrackNames.json")));
        foreach (string internalName in json.Keys)
        {
            string trackName = json[internalName].Value;
            MusicSwapperPlugin.Logger.LogMessage($"{internalName}, {trackName}");
            InternalNameToAlbumName.Add(internalName, trackName);
            //AlbumNameToInternalName.TryAdd(trackName, internalName);
        }
    }

    public static void SetAllTracks(HashSet<UnityObjectWrapperKey<MusicTrackDef>> allTracks)
    {
        TrackNameToMusicTrackDef = new(StringComparer.OrdinalIgnoreCase);
        //MusicTrackDefToTrackName = [];
        List<string> albumTrackNames = [];
        List<string> internalTrackNames = [];
        foreach (var musicTrack in allTracks.Select(x => x.value))
        {
            if (InternalNameToAlbumName.TryGetValue(musicTrack.cachedName, out var albumName))
            {
                if (!albumTrackNames.Contains(albumName))
                {
                    albumTrackNames.Add(albumName);
                }
                TrackNameToMusicTrackDef.TryAdd(albumName, musicTrack);
                //MusicTrackDefToTrackName.Add(musicTrack, albumName);
            }
            else
            {
                if (!internalTrackNames.Contains(musicTrack.cachedName))
                {
                    internalTrackNames.Add(musicTrack.cachedName);
                }
                TrackNameToMusicTrackDef.TryAdd(musicTrack.cachedName, musicTrack);
                //MusicTrackDefToTrackName.Add(musicTrack, musicTrack.cachedName);
            }
        }
        albumTrackNames.Sort();
        internalTrackNames.Sort();
        AllTrackTitles = [.. albumTrackNames, .. internalTrackNames];
        if (AllTrackTitles.Remove(Constants.NO_MUSIC))
        {
            AllTrackTitles.Add(Constants.NO_MUSIC);
        }
    }
}
