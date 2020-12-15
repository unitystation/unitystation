using System.Collections;
using UnityEngine;
using Mirror;
using Systems.GhostRoles;

namespace Messages.Server
{
	/// <summary>
	/// Sends a message to clients, informing them about a new ghost role that has become available.
	/// </summary>
	public class GhostRoleUpdateMessage : ServerMessage
	{
		public uint roleID;
		public int roleType;
		public int minPlayers;
		public int maxPlayers;
		public int playerCount;
		public float timeRemaining;

		// To be run on client
		public override void Process()
		{
			if (CustomNetworkManager.isHeadless || PlayerManager.LocalPlayer == null) return;

			if (!MatrixManager.IsInitialized) return;

			GhostRoleManager.Instance.ClientAddOrUpdateRole(roleID, roleType, minPlayers, maxPlayers, playerCount, timeRemaining);
		}

		/// <summary>
		/// Sends a message to all dead, informing them about a new ghost role that has become available.
		/// </summary>
		public static GhostRoleUpdateMessage SendToDead(uint key)
		{
			GhostRoleServer role = GhostRoleManager.Instance.serverAvailableRoles[key];

			foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
			{
				if (player.Script.IsDeadOrGhost == false) continue;
				SendTo(player, key, role);
			}

			return GetMessage(key, role);
		}

		/// <summary>
		/// Sends a message to the specific player, informing them about a new ghost role that has become available.
		/// </summary>
		public static GhostRoleUpdateMessage SendTo(ConnectedPlayer player, uint key, GhostRoleServer role)
		{
			GhostRoleUpdateMessage msg = GetMessage(key, role);
			msg.SendTo(player);
			return msg;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);

			roleID = reader.ReadUInt32();
			roleType = reader.ReadInt32();
			minPlayers = reader.ReadInt32();
			maxPlayers = reader.ReadInt32();
			playerCount = reader.ReadInt32();
			timeRemaining = reader.ReadSingle();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);

			writer.WriteUInt32(roleID);
			writer.WriteInt32(roleType);
			writer.WriteInt32(minPlayers);
			writer.WriteInt32(maxPlayers);
			writer.WriteInt32(playerCount);
			writer.WriteSingle(timeRemaining);
		}

		private static GhostRoleUpdateMessage GetMessage(uint key, GhostRoleServer role)
		{
			return new GhostRoleUpdateMessage
			{
				roleID = key,
				roleType = role.RoleListIndex,
				minPlayers = role.MinPlayers,
				maxPlayers = role.MaxPlayers,
				playerCount = role.WaitingPlayers.Count,
				timeRemaining = role.TimeRemaining,
			};
		}
	}
}
