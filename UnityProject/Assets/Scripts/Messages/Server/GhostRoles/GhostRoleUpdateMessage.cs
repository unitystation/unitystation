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
		public class GhostRoleUpdateMessageNetMessage : NetworkMessage
		{
			public uint roleID;
			public int roleType;
			public int minPlayers;
			public int maxPlayers;
			public int playerCount;
			public float timeRemaining;
		}

		// To be run on client
		public override void Process<T>(T msg)
		{
			var newMsg = msg as GhostRoleUpdateMessageNetMessage;
			if(newMsg == null) return;

			if (PlayerManager.LocalPlayer == null) return;

			if (MatrixManager.IsInitialized == false) return;

			GhostRoleManager.Instance.ClientAddOrUpdateRole(newMsg.roleID, newMsg.roleType, newMsg.minPlayers, newMsg.maxPlayers, newMsg.playerCount, newMsg.timeRemaining);
		}

		/// <summary>
		/// Sends a message to all dead, informing them about a new ghost role that has become available.
		/// </summary>
		public static GhostRoleUpdateMessageNetMessage SendToDead(uint key)
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

			return null;


		}

		/// <summary>
		/// Sends a message to the specific player, informing them about a new ghost role that has become available.
		/// </summary>
		public static GhostRoleUpdateMessageNetMessage SendTo(ConnectedPlayer player, uint key, GhostRoleServer role)
		{
			GhostRoleUpdateMessageNetMessage msg = GetMessage(key, role);
			new GhostRoleUpdateMessage().SendTo(player, msg);
			return msg;
		}

		private static GhostRoleUpdateMessageNetMessage GetMessage(uint key, GhostRoleServer role)
		{
			return new GhostRoleUpdateMessageNetMessage
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
