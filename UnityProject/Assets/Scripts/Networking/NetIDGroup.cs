using System;
using System.Collections.Generic;

/// <summary>
/// An easy way to serialize a collection of NetID's into JSON or other formats.
/// Use new NetworkIdentity(NetIDs[i]) to convert back to NetId.
/// </summary>
[Serializable]
public class NetIDGroup
{
    /// <summary>
    /// The group of uint's representing the NetID values
    /// </summary>
    /// <typeparam name="uint"> Get Via NetId.Value</typeparam>
    public List<uint> NetIDs = new List<uint>();

    public NetIDGroup() { }
    public NetIDGroup(List<ConnectedPlayer> group)
    {
        foreach (ConnectedPlayer c in group)
        {
            if (c.Script != null)
            {
                NetIDs.Add(c.Script.netId.Value);
            }
        }
    }
}