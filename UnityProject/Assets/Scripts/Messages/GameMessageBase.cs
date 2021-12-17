using System;
using Managers;
using Mirror;
using UnityEngine;

namespace Messages
{
	public abstract class GameMessageBase<T> where T : struct, NetworkMessage
	{
		public GameObject NetworkObject;
		public GameObject[] NetworkObjects;

		/// <summary>
		/// Called before any message processing takes place
		/// </summary>
		public virtual void PreProcess(NetworkConnection sentBy, T b)
		{
			InfiniteLoopTracker.gameMessageProcessing = true;
			InfiniteLoopTracker.lastGameMessage = ToString();
			Process(sentBy, b);
			InfiniteLoopTracker.gameMessageProcessing = false;
		}

		public abstract void Process(T msg);

		public virtual void Process(NetworkConnection sentBy, T msg)
		{
			// This is to stop the server disconnecting if theres an error in the processing of the net message
			// on either client or server side
			try
			{
				Process(msg);
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
		}

		protected bool LoadNetworkObject(uint id)
		{
			if (NetworkIdentity.spawned.ContainsKey(id) && NetworkIdentity.spawned[id] != null)
			{
				NetworkObject = NetworkIdentity.spawned[id].gameObject;
				return true;
			}
			NetworkObject = null;
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
}
