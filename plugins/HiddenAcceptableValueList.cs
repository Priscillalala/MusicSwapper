using BepInEx.Configuration;

namespace MusicSwapper;

public class HiddenAcceptableValueList<T>(T[] acceptableValues, HashSet<T> hiddenAcceptableValues) 
    : AcceptableValueList<T>(acceptableValues) where T : IEquatable<T>
{
    public virtual HashSet<T> HiddenAcceptableValues { get; } = hiddenAcceptableValues;

    public override bool IsValid(object value)
    {
        if (value is T tValue && HiddenAcceptableValues.Contains(tValue))
        {
            return true;
        }
        return base.IsValid(value);
    }
}
