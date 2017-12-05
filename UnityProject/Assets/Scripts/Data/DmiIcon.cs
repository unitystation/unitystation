using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class DmiIcon
{
    public string icon;
    public List<DmiState> states;

    private Sprite[] sprites = { };

    public Sprite[] spriteSheet
    {
        get { return sprites; }
        set { sprites = value; }
    }

    public string getName()
    {
        if (icon != null && icon.Contains(".dmi"))
        {
            int startIndex = icon.LastIndexOf('/') + 1;
            int endIndex = icon.IndexOf(".dmi", StringComparison.Ordinal);
            return icon.Substring(startIndex, endIndex - startIndex);
        }
        //        Debug.LogWarning("getName: something's wrong");
        return "";
    }

    public DmiState getState(string state)
    {
        var foundState = states.Find(x => x.state == state);
        if (foundState != null)
        {
            //                Debug.Log("foundState: "+ foundState);
            return foundState;
        }

        //        Debug.LogWarning("Couldn't find dmiIcon by state " + state);
        return new DmiState();
    }

    public DmiState getStateAtOffset(int offset, out int relativeOffset)
    {
        relativeOffset = 0;
        if (!offset.Equals(-1))
        {
            var foundState = states.Find(x => x.OwnsOffset(offset));
            if (foundState != null)
            {
                relativeOffset = foundState.GetRelativeOffset(offset);
                return foundState;
            }
        }
        return new DmiState();
    }


    /// <returns> -1 if you feed custom shit to it</returns>
    internal static int getOffsetFromUnityName(string unityName)
    {
        var intStr = unityName.Split('_');
        var last = intStr.Last();
        int offset = int.TryParse(last, out offset) ? offset : -1;
        return offset;
    }


    public DmiIcon(string icon, List<DmiState> states)
    {
        this.icon = icon;
        this.states = states;
    }

    public DmiIcon(string icon) : this(icon, new List<DmiState>())
    {
    }

    public DmiIcon() : this("")
    {
    }

    protected bool Equals(DmiIcon other)
    {
        return string.Equals(icon, other.icon);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DmiIcon)obj);
    }

    public override int GetHashCode()
    {
        return icon.GetHashCode();
    }

    public override string ToString()
    {
        var state = states.Aggregate("", (current, ds) => current + ds.ToString());
        return string.Format("Icon: {0}, States: {{{1}}};", icon, state);
    }
}