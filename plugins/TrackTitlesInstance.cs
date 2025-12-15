using HG;
using RoR2;
using SimpleJSON;
using Path = System.IO.Path;

namespace MusicSwapper;

public class TrackTitlesInstance
{
    public Dictionary<string, string> InternalNameToAlbumName { get; private set; }
    public List<TrackTitle> AllTrackTitles { get; private set; }
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
        TrackNameToMusicTrackDef = new(StringComparer.OrdinalIgnoreCase);
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
            }
            else
            {
                if (!internalTrackNames.Contains(musicTrack.cachedName))
                {
                    internalTrackNames.Add(musicTrack.cachedName);
                }
                TrackNameToMusicTrackDef.TryAdd(musicTrack.cachedName, musicTrack);
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
