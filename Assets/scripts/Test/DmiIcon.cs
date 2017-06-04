using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class DmiIcon
{
    public string icon;
    public List<DmiState> states;

    private Sprite[] sprites;

    public Sprite[] spriteSheet
    {
        get { return sprites; }
        set { sprites = value; }
    }

    public DmiIcon(string icon, List<DmiState> states)
    {
        this.icon = icon;
        this.states = states;
    }

    public DmiIcon(string icon) : this(icon, null)
    {
    }

    public DmiIcon() : this(null, null)
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
        return Equals((DmiIcon) obj);
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