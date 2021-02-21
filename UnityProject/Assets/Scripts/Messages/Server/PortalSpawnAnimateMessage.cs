using System.Collections;
using UnityEngine;
using Systems.Spells.Wizard;
using Mirror;

namespace Messages.Server
{
	/// <summary>
	/// Sends a message to nearby clients, informing them to animate the given portal with the given settings.
	/// </summary>
	public class PortalSpawnAnimateMessage : ServerMessage
	{
		public class PortalSpawnAnimateMessageNetMessage : NetworkMessage
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
		public static PortalSpawnAnimateMessageNetMessage SendToVisible(GameObject entity, PortalSpawnInfo settings, AnimateType type)
		{
			var msg = new PortalSpawnAnimateMessageNetMessage()
			{
				Entity = entity,
				Settings = settings,
				Type = type,
			};

			new PortalSpawnAnimateMessage().SendToVisiblePlayers(entity.RegisterTile().WorldPositionServer.To2Int(), msg);
			return msg;
		}

		public override void Process<T>(T msg)
		{
			var newMsg = msg as PortalSpawnAnimateMessageNetMessage;
			if(newMsg == null) return;

			if (newMsg.Type == AnimateType.Portal)
			{
				SpawnByPortal.AnimatePortal(newMsg.Entity, newMsg.Settings);
			}
			else
			{
				SpawnByPortal.AnimateObject(newMsg.Entity, newMsg.Settings);
			}
		}

		public enum AnimateType
		{
			Portal,
			Entity,
		}
	}
}
