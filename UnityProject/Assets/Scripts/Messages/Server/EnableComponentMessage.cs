using System;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Quick 'n dirty net message to set a component's enabled / disabled state.
	/// Won't work as intended if we ever dynamically add / remove components unsynced,
	/// and new-joins / rejoins won't reflect the current server state, so use with caution.
	/// </summary>
	public class EnableComponentMessage : ServerMessage<EnableComponentMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint GameObject;
			public int ComponentIndex;
			public string ComponentName;
			public bool SetActive;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.GameObject);
			var components = NetworkObject.GetComponents<Behaviour>();
			if (msg.ComponentIndex < components.Length)
			{
				var component = components[msg.ComponentIndex];
				// probably shouldn't assume the component list is identical for each client and server,
				// so run a lil sanity check
				if (component.GetType().Name == msg.ComponentName)
				{
					component.enabled = msg.SetActive;
				}
			}
		}

		public static void Send(Behaviour toEnable, bool setActive)
		{
			var components = toEnable.gameObject.GetComponents<Behaviour>();

			var msg = new NetMessage
			{
				GameObject = toEnable.gameObject.NetId(),
				ComponentIndex = Array.IndexOf(components, toEnable),
				ComponentName = toEnable.GetType().Name,
				SetActive = setActive,
			};

			if (msg.ComponentIndex < 0) return;

			SendToAll(msg);
		}
	}
}
