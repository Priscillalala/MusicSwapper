using HG;
using RoR2;
using SimpleJSON;
using Path = System.IO.Path;

namespace MusicSwapper;

public class TrackTitlesInstance
{
    public Dictionary<string, string> InternalNameToAlbumName { get; private set; }
    public List<TrackTitle> PrimaryTrackTitles { get; private set; }
    public HashSet<TrackTitle> AllTrackTitles { get; private set; }
    public Dictionary<string, MusicTrackDef> TrackNameToMusicTrackDef { get; private set; }

    public TrackTitlesInstance(string fileName)
    {
        InternalNameToAlbumName = [];
        string path = Path.Combine(MusicSwapperPlugin.RuntimeDirectory, fileName);
        if (!File.Exists(path))
        {
            MusicSwapperPlugin.Logger.LogWarning($"Could not find {fileName}!");
            return;
        }
        var json = JSON.Parse(File.ReadAllText(path));
        foreach (string internalName in json.Keys)
        {
            string trackName = json[internalName].Value;
            InternalNameToAlbumName.Add(internalName, trackName);
        }
        MusicSwapperPlugin.Logger.LogMessage($"Loaded {json.Count} track titles from {fileName}");
    }

    public void SetAllTracks(HashSet<UnityObjectWrapperKey<MusicTrackDef>> allTracks)
    {
        AllTrackTitles = [];
        TrackNameToMusicTrackDef = [];
        List<string> albumTrackNames = [];
        List<string> internalTrackNames = [];
        foreach (var musicTrack in allTracks.Select(x => x.value))
        {
            string internalName = musicTrack.cachedName;
            if (InternalNameToAlbumName.TryGetValue(internalName, out var albumName))
            {
                if (!albumTrackNames.Contains(albumName))
                {
                    albumTrackNames.Add(albumName);
                }
                AllTrackTitles.Add(albumName);
                TrackNameToMusicTrackDef.TryAdd(albumName, musicTrack);
            }
            else if (!internalTrackNames.Contains(internalName))
            {
                internalTrackNames.Add(internalName);
            }
            AllTrackTitles.Add(internalName);
            TrackNameToMusicTrackDef.TryAdd(internalName, musicTrack);
        }
        albumTrackNames.Sort();
        internalTrackNames.Sort();
        PrimaryTrackTitles = [.. albumTrackNames, .. internalTrackNames];
        if (PrimaryTrackTitles.Remove(Constants.TRACK_NAME_NONE))
        {
            PrimaryTrackTitles.Add(Constants.TRACK_NAME_NONE);
        }
    }
}
