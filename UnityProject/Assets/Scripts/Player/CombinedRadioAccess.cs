using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombinedRadioAccess : MonoBehaviour
{

	public List<BodyPartRadioAccess> ConnectedAccess = new List<BodyPartRadioAccess>();


	public void AddAccess(BodyPartRadioAccess access)
	{
		if (ConnectedAccess.Contains(access) == false)
		{
			ConnectedAccess.Add(access);
		}
	}

	public void RemoveAccess(BodyPartRadioAccess access)
	{
		if (ConnectedAccess.Contains(access))
		{
			ConnectedAccess.Remove(access);
		}
	}

	public ChatChannel GetChannels()
	{
		ChatChannel channel = ChatChannel.None;
		foreach (BodyPartRadioAccess access in ConnectedAccess)
		{
			channel = channel | access.AvailableChannels;
		}
		return channel;
	}


}
