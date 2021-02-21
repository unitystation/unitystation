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

	/// <summary>
	/// Called before any message processing takes place
	/// </summary>
	public virtual void PreProcess<T>(NetworkConnection sentBy, T b) where T : NetworkMessage
	{
		Process(sentBy, b);
	}

	public abstract void Process<T>(T msg) where T : NetworkMessage;

	public virtual void Process<T>( NetworkConnection sentBy, T msg ) where T : NetworkMessage
	{
		Process(msg);
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
