using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Mirror;

public abstract class GameMessageBase : MessageBase
{
	public GameObject NetworkObject;
	public List<GameObject> NetworkObjects = new List<GameObject>();

	/// <summary>
	/// Called before any message processing takes place
	/// </summary>
	public virtual void PreProcess(NetworkConnection sentBy, GameMessageBase b)
	{
		b.Process(sentBy);
	}

	public abstract void Process();

	public virtual void Process( NetworkConnection sentBy )
	{
		Process();
	}

	protected bool LoadNetworkObject(uint id)
	{
		if (NetworkIdentity.spawned.ContainsKey(id))
		{
			NetworkObject = NetworkIdentity.spawned[id].gameObject;
			return true;
		}

		return false;
	}

	protected void LoadMultipleObjects(uint[] ids)
	{
		for (int i = 0; i < ids.Length; i++)
		{
			NetworkObjects.Add(null);
			var netId = ids[i];
			if (NetworkIdentity.spawned.ContainsKey(netId))
			{
				NetworkObjects[i] = NetworkIdentity.spawned[netId].gameObject;
			}
		}
	}
}
