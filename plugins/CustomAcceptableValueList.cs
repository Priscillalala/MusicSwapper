using BepInEx.Configuration;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicSwapper;

public class CustomAcceptableValueList(TrackTitle[] acceptableValues, IEqualityComparer<string> valueComparer) : AcceptableValueList<TrackTitle>(acceptableValues)
{
    public virtual IEqualityComparer<string> ValueComparer { get; } = valueComparer;

    public override bool IsValid(object value)
    {
        if (value is TrackTitle trackTitle)
        {
            
        }
        return false;
    }
}
