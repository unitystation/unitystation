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
			Process(sentBy, b);
		}

		public abstract void Process(T msg);

		public virtual void Process(NetworkConnection sentBy, T msg)
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
}
