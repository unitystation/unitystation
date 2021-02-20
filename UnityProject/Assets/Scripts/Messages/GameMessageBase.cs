using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Mirror;

public abstract class GameMessageBase
{
	public GameObject NetworkObject;
	public GameObject[] NetworkObjects;

	public struct NetMessage : NetworkMessage
	{
		public ActualMessage actualMessage;
	}

	[Serializable]
	public class ActualMessage {}

	public NetMessage netMessage;

	public abstract void Process(ActualMessage msg);

	public virtual void Process( NetworkConnection sentBy, NetMessage msg )
	{
		Process(msg.actualMessage);
	}

	protected bool LoadNetworkObject(uint id)
	{
		if (NetworkIdentity.spawned.ContainsKey(id) && NetworkIdentity.spawned[id] != null)
		{
			NetworkObject = NetworkIdentity.spawned[id].gameObject;
			return true;
		}

		return false;
	}

	protected void LoadMultipleObjects(uint[] ids)
	{
		NetworkObjects = new GameObject[ids.Length];
		for (int i = 0; i < ids.Length; i++)
		{
			var netId = ids[i];
			if (NetworkIdentity.spawned.ContainsKey(netId) && NetworkIdentity.spawned[netId] != null)
			{
				NetworkObjects[i] = NetworkIdentity.spawned[netId].gameObject;
			}
		}
	}
}
