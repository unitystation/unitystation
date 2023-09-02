using Antagonists;
using Logs;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestRespawnPlayer : ClientMessage<RequestRespawnPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string UserToRespawn;
			public string OccupationToRespawn;
			public int Type;
		}

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			if (PlayerList.Instance.TryGetByUserID(msg.UserToRespawn, out var player) == false || player.Script == null)
			{
				Loggy.LogError($"Player with user ID '{msg.UserToRespawn}' not found or has no script. Cannot respawn player.", Category.Admin);
				return;
			}

			// Wasn't so dead, let's kill them
			if (player.Script.playerHealth != null &&
				player.Script.playerHealth.IsDead == false)
			{
				player.Script.playerHealth.ApplyDamageAll(SentByPlayer.GameObject, 200, AttackType.Internal, DamageType.Brute);
			}

			TryRespawn(player, msg, msg.OccupationToRespawn);
		}

		private void TryRespawn(PlayerInfo deadPlayer, NetMessage msg, string occupation = null)
		{
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
					$"{SentByPlayer.Username} respawned dead player {deadPlayer.Username} ({deadPlayer.Name}) as {occupation}",
					SentByPlayer.UserId);

			var respawnType = (RespawnType) msg.Type;

			switch (respawnType)
			{
				case RespawnType.Normal:
					deadPlayer.Script.PlayerNetworkActions.ServerRespawnPlayer(occupation);
					break;
				case RespawnType.Special:
					deadPlayer.Script.PlayerNetworkActions.ServerRespawnPlayerSpecial(occupation);
					break;
				case RespawnType.Antag:
					deadPlayer.Script.PlayerNetworkActions.ServerRespawnPlayerAntag(deadPlayer, occupation);
					break;
			}
		}

		public static NetMessage SendNormalRespawn(string userIDToRespawn, Occupation occupation)
		{
			var msg = new NetMessage
			{
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 0
			};

			Send(msg);
			return msg;
		}

		public static NetMessage SendSpecialRespawn(string userIDToRespawn, Occupation occupation)
		{
			var msg = new NetMessage()
			{
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 1
			};

			Send(msg);
			return msg;
		}

		public static NetMessage SendAntagRespawn(string userIdTorespawn, Antagonist antagonist)
		{
			var msg = new NetMessage()
			{
				UserToRespawn = userIdTorespawn,
				OccupationToRespawn = antagonist.AntagName,
				Type = 2
			};

			Send(msg);
			return msg;
		}

		public enum RespawnType
		{
			Normal = 0,
			Special = 1,
			Antag = 2
		}
	}
}
