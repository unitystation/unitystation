﻿using Systems.GhostRoles;
using Mirror;

namespace Messages.Server.GhostRoles
{
	/// <summary>
	/// Sends a message to clients, informing them about a new ghost role that has become available.
	/// </summary>
	public class GhostRoleUpdateMessage : ServerMessage<GhostRoleUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint roleID;
			public int roleType;
			public int minPlayers;
			public int maxPlayers;
			public int playerCount;
			public float timeRemaining;
		}

		// To be run on client
		public override void Process(NetMessage msg)
		{
			if (PlayerManager.LocalPlayer == null) return;

			if (MatrixManager.IsInitialized == false) return;

			GhostRoleManager.Instance.ClientAddOrUpdateRole(msg.roleID, msg.roleType, msg.minPlayers, msg.maxPlayers, msg.playerCount, msg.timeRemaining);
		}

		/// <summary>
		/// Sends a message to all dead, informing them about a new ghost role that has become available.
		/// </summary>
		public static NetMessage SendToDead(uint key)
		{
			if (GhostRoleManager.Instance != null)
			{
				GhostRoleServer role = GhostRoleManager.Instance.serverAvailableRoles[key];

				foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
				{
					if (player?.Script == null) { Logger.LogError("SendToDead, player?.Script == null"); continue; }
					if (player.Script.IsDeadOrGhost == false) continue;
					SendTo(player, key, role);
				}
				return GetMessage(key, role);
			}
			else
			{
				Logger.LogError("SendToDead, GhostRoleManager.Instance == null");
			}

			return new NetMessage();
		}

		/// <summary>
		/// Sends a message to the specific player, informing them about a new ghost role that has become available.
		/// </summary>
		public static NetMessage SendTo(ConnectedPlayer player, uint key, GhostRoleServer role)
		{
			NetMessage msg = GetMessage(key, role);

			SendTo(player, msg);
			return msg;
		}

		private static NetMessage GetMessage(uint key, GhostRoleServer role)
		{
			return new NetMessage
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
