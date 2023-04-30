using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CombinedRadioAccess : NetworkBehaviour
{

	public List<BodyPartRadioAccess> ConnectedAccess = new List<BodyPartRadioAccess>();


	[SyncVar]
	public ChatChannel AccessToChannel = ChatChannel.None;


	public void AddAccess(BodyPartRadioAccess access)
	{
		if (ConnectedAccess.Contains(access) == false)
		{
			ConnectedAccess.Add(access);
		}

		ChatChannel channel = ChatChannel.None;
		foreach (BodyPartRadioAccess Inaccess in ConnectedAccess)
		{
			channel = channel | Inaccess.AvailableChannels;
		}

		AccessToChannel = channel;
	}

	public void RemoveAccess(BodyPartRadioAccess access)
	{
		if (ConnectedAccess.Contains(access))
		{
			ConnectedAccess.Remove(access);
		}
		ChatChannel channel = ChatChannel.None;
		foreach (BodyPartRadioAccess Inaccess in ConnectedAccess)
		{
			channel = channel | Inaccess.AvailableChannels;
		}

		AccessToChannel = channel;
	}

	public ChatChannel GetChannels()
	{
		return AccessToChannel;
	}


}
