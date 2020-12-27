using System.Collections;
using UnityEngine;
using Mirror;
using Systems.Spells.Wizard;

namespace Messages.Server
{
	/// <summary>
	/// Sends a message to nearby clients, informing them to animate the given portal with the given settings.
	/// </summary>
	public class PortalSpawnAnimateMessage : ServerMessage
	{
		public GameObject Entity;
		public PortalSpawnInfo Settings;
		public AnimateType Type;

		/// <summary>
		/// <inheritdoc cref="PortalSpawnAnimateMessage"/>
		/// </summary>
		/// <param name="portal">The portal GameObject that the client should animate.</param>
		/// <param name="settings">The settings the portal should be animated with.</param>
		/// <returns></returns>
		public static PortalSpawnAnimateMessage SendToVisible(GameObject entity, PortalSpawnInfo settings, AnimateType type)
		{
			var msg = new PortalSpawnAnimateMessage()
			{
				Entity = entity,
				Settings = settings,
				Type = type,
			};

			msg.SendToVisiblePlayers(entity.RegisterTile().WorldPositionServer.To2Int());

			return msg;
		}

		public override void Process()
		{
			if (Type == AnimateType.Portal)
			{
				SpawnByPortal.AnimatePortal(Entity, Settings);
			}
			else
			{
				SpawnByPortal.AnimateObject(Entity, Settings);
			}
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);

			Entity = reader.ReadGameObject();
			Settings.PortalHeight = reader.ReadInt32();
			Settings.PortalOpenTime = reader.ReadSingle();
			Settings.PortalCloseTime = reader.ReadSingle();
			Settings.PortalSuspenseTime = reader.ReadSingle();
			Settings.EntityRotate = reader.ReadBoolean();
			Type = (AnimateType)reader.ReadInt32();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);

			writer.WriteGameObject(Entity);
			writer.WriteInt32(Settings.PortalHeight);
			writer.WriteSingle(Settings.PortalOpenTime);
			writer.WriteSingle(Settings.PortalCloseTime);
			writer.WriteSingle(Settings.PortalSuspenseTime);
			writer.WriteBoolean(Settings.EntityRotate);
			writer.WriteInt32((int)Type);
		}

		public enum AnimateType
		{
			Portal,
			Entity,
		}
	}
}
