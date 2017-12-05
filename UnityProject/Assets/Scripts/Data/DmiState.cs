using System;
using System.Runtime.InteropServices;

[Serializable]
public class DmiState
{
    public string unityName;
    public string state;
    public string delay;
    public int offset = -1;
    public int frames;
    public int dirs;

    public DmiState(string unityName, string state, string delay, int offset, int frames, int dirs)
    {
        this.unityName = unityName;
        this.state = state;
        this.delay = delay;
        this.offset = offset;
        this.frames = frames;
        this.dirs = dirs;
    }

    public DmiState(string state) : this("", state, "", -1, -1, -1)
    {
    }
    public DmiState() : this("")
    {
    }

    //    public string GetUnityNameForRelOffset(int relativeOffset)
    //    {
    //        var subjectOffset = offset + relativeOffset;
    //        if (unityName.Contains("_") && OwnsOffset(subjectOffset))
    //        {
    //            var uName = unityName.Substring(0, unityName.LastIndexOf('_')+1);
    //            return uName + subjectOffset;
    //        }
    //        return "";
    //    }

    public int GetRelativeOffset(int iconOffset)
    {
        if (OwnsOffset(iconOffset))
        {
            return iconOffset - offset;
        }
        return -1;
    }

    public bool OwnsOffset(int offset)
    {
        return this.offset.Equals(offset) || offset > this.offset && offset <= endOffset;
    }

    private int endOffset => offset + (frames * dirs - 1);

    protected bool Equals(DmiState other)
    {
        return string.Equals(state, other.state) && dirs == other.dirs;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DmiState)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((state != null ? state.GetHashCode() : 0) * 397) ^ dirs;
        }
    }

    public override string ToString()
    {
        return string.Format("UnityName: {0}, State: {1}, Delay: {2}, Offset: {3}, Frames: {4}, Dirs: {5};", unityName, state, delay, offset, frames, dirs);
    }
}