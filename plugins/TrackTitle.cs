using BepInEx.Configuration;

namespace MusicSwapper;

// Used for config entries. Normal strings are escaped when written to the config so songs with commas would not appear correctly
public readonly struct TrackTitle(string value) : IEquatable<TrackTitle>
{
    public string Value { get; } = value;

    static TrackTitle()
    {
        TomlTypeConverter.AddConverter(typeof(TrackTitle), new TypeConverter
        {
            ConvertToString = (obj, type) => obj.ToString(),
            ConvertToObject = (str, type) => new TrackTitle(str)
        });
    }

    public override readonly string ToString() => Value;

    public static implicit operator TrackTitle(string value) => new TrackTitle(value);

    public static implicit operator string(TrackTitle trackTitle) => trackTitle.Value;

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    public bool Equals(string other) => string.Equals(Value, other);

    public bool Equals(TrackTitle other) => Equals(other.Value);

    public override bool Equals(object other) => other switch
    {
        TrackTitle trackTitle => Equals(trackTitle),
        string value => Equals(value),
        _ => false
    };
}
