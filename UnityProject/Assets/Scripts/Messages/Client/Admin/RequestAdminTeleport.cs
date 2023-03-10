using UnityEngine;
using Mirror;


namespace Messages.Client.Admin
{
	public class RequestAdminTeleport : ClientMessage<RequestAdminTeleport.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string UserToTeleport;
			public string UserToTeleportTo;
			public OpperationList OpperationNumber;
			public bool IsAghost;
			public float vectorX;
			public float vectorY;
			public float vectorZ;
		}

		public override void Process(NetMessage msg)
		{
			switch (msg.OpperationNumber)
			{
				case OpperationList.AdminToPlayer:
					DoAdminToPlayerTeleport(msg);
					return;
				case OpperationList.PlayerToAdmin:
					DoPlayerToAdminTeleport(msg);
					return;
				case OpperationList.AllPlayersToPlayer:
					DoAllPlayersToPlayerTeleport(msg);
					return;
			}
		}

		private void DoPlayerToAdminTeleport(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			PlayerScript userToTeleport = null;

			foreach (var player in PlayerList.Instance.AllPlayers)
			{
				if (player.AccountId == msg.UserToTeleport)
				{
					userToTeleport = player.Script;

					break;
				}
			}

			if (userToTeleport == null) return;

			var coord = new Vector3 {x = msg.vectorX, y = msg.vectorY, z = msg.vectorZ };

			if (userToTeleport.PlayerSync != null)
			{
				userToTeleport.PlayerSync.AppearAtWorldPositionServer(coord, false);
			}
			else if(userToTeleport.TryGetComponent<GhostMove>(out var ghostMove))
			{
				ghostMove.ForcePositionClient(coord, false, false);
			}

			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
					$"{SentByPlayer.Username} teleported {userToTeleport.playerName} to themselves", SentByPlayer.AccountId);
		}

		private void DoAdminToPlayerTeleport(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			PlayerScript userToTeleportTo = null;

			foreach (var player in PlayerList.Instance.AllPlayers)
			{
				if (player.AccountId == msg.UserToTeleportTo)
				{
					userToTeleportTo = player.Script;

					break;
				}
			}

			if (userToTeleportTo == null) return;

			var playerScript = SentByPlayer.Script;

			if (playerScript == null) return;

			if (playerScript.PlayerSync != null)
			{
				playerScript.PlayerSync.AppearAtWorldPositionServer(userToTeleportTo.gameObject.AssumedWorldPosServer(), false);
			}
			else if(playerScript.TryGetComponent<GhostMove>(out var ghostMove))
			{
				ghostMove.ForcePositionClient(userToTeleportTo.gameObject.AssumedWorldPosServer(), false, false);
			}

			string message;

			if (msg.IsAghost)
			{
				message = $"{SentByPlayer.Username} teleported to {userToTeleportTo.playerName} as a ghost";
			}
			else
			{
				message = $"{SentByPlayer.Username} teleported to {userToTeleportTo.playerName} as a player";
			}

			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(message, SentByPlayer.AccountId);
		}

		private void DoAllPlayersToPlayerTeleport(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			PlayerScript destinationPlayer = null;

			foreach (var player in PlayerList.Instance.AllPlayers)
			{
				if (player.AccountId == msg.UserToTeleportTo)
				{
					destinationPlayer = player.Script;

					break;
				}
			}

			if (destinationPlayer == null) return;

			foreach (var player in PlayerList.Instance.AllPlayers)
			{
				PlayerScript userToTeleport = player.Script;

				if (userToTeleport == null) continue;

				if (msg.IsAghost)
				{
					var coord = new Vector3 { x = msg.vectorX, y = msg.vectorY, z = msg.vectorZ };

					userToTeleport.OrNull()?.PlayerSync.OrNull()?.AppearAtWorldPositionServer(coord, false);
				}
				else if (destinationPlayer.IsGhost)
				{
					//if the  destination player player is a ghost the system breaks as for some reason ghost position is not accurate on server.
					//To test for future reference: test coord on headless, works fine in editor.
					//if admin is ghost top condition is used as the admin can pass their position from client to server.
					return;
				}
				else
				{
					userToTeleport.OrNull()?.PlayerSync.OrNull()?.AppearAtWorldPositionServer(destinationPlayer.gameObject.AssumedWorldPosServer(), false);
				}
			}

			var stringMsg = $"{SentByPlayer.Username} teleported all players to {destinationPlayer.playerName}";

			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(stringMsg, SentByPlayer.AccountId);
		}

		public static NetMessage Send(string userToTeleport, string userToTelportTo, OpperationList opperation, bool isAghost, Vector3 Coord)
		{
			NetMessage msg = new NetMessage
			{
				UserToTeleport = userToTeleport,
				UserToTeleportTo = userToTelportTo,
				OpperationNumber = opperation,
				IsAghost = isAghost,
				vectorX = Coord.x,
				vectorY = Coord.y,
				vectorZ = Coord.z
			};

			Send(msg);
			return msg;
		}

		public enum OpperationList
		{
			AdminToPlayer = 1,
			PlayerToAdmin = 2,
			AllPlayersToPlayer = 3
		}
	}
}
