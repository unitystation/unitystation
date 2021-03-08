using Antagonists;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestRespawnPlayer : ClientMessage<RequestRespawnPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string UserToRespawn;
			public string OccupationToRespawn;
			public int Type;
		}

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		void VerifyAdminStatus(NetMessage msg)
		{
			var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (player == null) return;
			var deadPlayer = PlayerList.Instance.GetByUserID(msg.UserToRespawn);
			if (deadPlayer == null || deadPlayer.Script == null) return;

			//Wasn't so dead, let's kill them
			if (deadPlayer.Script.playerHealth != null &&
			    deadPlayer.Script.playerHealth.IsDead == false)
			{
				deadPlayer.Script.playerHealth.ApplyDamageAll(
					player,
					200,
					AttackType.Internal,
					DamageType.Brute);
			}

			TryRespawn(deadPlayer, msg, msg.OccupationToRespawn);
		}

		void TryRespawn(ConnectedPlayer deadPlayer, NetMessage msg, string occupation = null)
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{PlayerList.Instance.GetByUserID(msg.Userid).Name} respawned dead player {deadPlayer.Name} as {occupation}", msg.Userid);

			var respawnType = (RespawnType) msg.Type;

			switch (respawnType)
			{
				case RespawnType.Normal:
					deadPlayer.Script.playerNetworkActions.ServerRespawnPlayer(occupation);
					break;
				case RespawnType.Special:
					deadPlayer.Script.playerNetworkActions.ServerRespawnPlayerSpecial(occupation);
					break;
				case RespawnType.Antag:
					deadPlayer.Script.playerNetworkActions.ServerRespawnPlayerAntag(deadPlayer, occupation);
					break;
			}
		}

		public static NetMessage SendNormalRespawn(string userId, string adminToken, string userIDToRespawn,
			Occupation occupation)
		{
			var msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 0
			};

			Send(msg);
			return msg;
		}

		public static NetMessage SendSpecialRespawn(string userID, string adminToken,
			string userIDToRespawn,
			Occupation occupation)
		{
			var msg = new NetMessage()
			{
				Userid = userID,
				AdminToken = adminToken,
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 1
			};

			Send(msg);
			return msg;
		}

		public static NetMessage SendAntagRespawn(string userID, string adminToken, string userIdTorespawn,
			Antagonist antagonist)
		{
			var msg = new NetMessage()
			{
				Userid = userID,
				AdminToken = adminToken,
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
