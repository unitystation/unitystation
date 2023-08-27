using System;
using Managers;
using Mirror;
using SecureStuff;
using UnityEngine;

namespace Messages
{
	public abstract class GameMessageBase<T> : IAllowedReflection where T : struct, NetworkMessage
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
			InfiniteLoopTracker.NetNetworkMessage = b;
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
			var spawned =
				CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

			if (spawned.TryGetValue(id, out var networkIdentity) && networkIdentity != null)
			{
				NetworkObject = networkIdentity.gameObject;
				return true;
			}

			NetworkObject = null;
			return false;
		}

		protected void LoadMultipleObjects(uint[] ids)
		{
			var spawned =
				CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

			NetworkObjects = new GameObject[ids.Length];
			for (int i = 0; i < ids.Length; i++)
			{
				var netId = ids[i];
				if (spawned.TryGetValue(netId, out var networkIdentity) && networkIdentity != null)
				{
					NetworkObjects[i] = networkIdentity.gameObject;
				}
			}
		}
	}
}
