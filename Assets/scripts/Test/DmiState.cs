using System;

[Serializable]
public class DmiState
{
    public string unityName;
    public string state;
    public string delay;
    public int offset;
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

    public DmiState()
    {
    }

    public override string ToString()
    {
        return string.Format("UnityName: {0}, State: {1}, Delay: {2}, Offset: {3}, Frames: {4}, Dirs: {5};", unityName, state, delay, offset, frames, dirs);
    }
}