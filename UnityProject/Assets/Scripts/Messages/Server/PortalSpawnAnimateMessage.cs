using System.Collections;
using UnityEngine;
using Systems.Spells.Wizard;
using Mirror;

namespace Messages.Server
{
	/// <summary>
	/// Sends a message to nearby clients, informing them to animate the given portal with the given settings.
	/// </summary>
	public class PortalSpawnAnimateMessage : ServerMessage<PortalSpawnAnimateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public GameObject Entity;
			public PortalSpawnInfo Settings;
			public AnimateType Type;
		}

		/// <summary>
		/// <inheritdoc cref="PortalSpawnAnimateMessage"/>
		/// </summary>
		/// <param name="portal">The portal GameObject that the client should animate.</param>
		/// <param name="settings">The settings the portal should be animated with.</param>
		/// <returns></returns>
		public static NetMessage SendToVisible(GameObject entity, PortalSpawnInfo settings, AnimateType type)
		{
			var msg = new NetMessage()
			{
				Entity = entity,
				Settings = settings,
				Type = type,
			};

			SendToVisiblePlayers(entity.RegisterTile().WorldPositionServer.To2Int(), msg);
			return msg;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.Type == AnimateType.Portal)
			{
				SpawnByPortal.AnimatePortal(msg.Entity, msg.Settings);
			}
			else
			{
				SpawnByPortal.AnimateObject(msg.Entity, msg.Settings);
			}
		}

		public enum AnimateType
		{
			Portal,
			Entity,
		}
	}
}
