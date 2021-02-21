using Antagonists;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestRespawnPlayer : ClientMessage
	{
		public class RequestRespawnPlayerNetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string UserToRespawn;
			public string OccupationToRespawn;
			public int Type;
		}

		public override void Process<T>(T msg)
		{
			var newMsg = msg as RequestRespawnPlayerNetMessage;
			if(newMsg == null) return;

			VerifyAdminStatus(newMsg);
		}

		void VerifyAdminStatus(RequestRespawnPlayerNetMessage msg)
		{
			var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (player == null) return;
			var deadPlayer = PlayerList.Instance.GetByUserID(msg.UserToRespawn);
			if (deadPlayer == null || deadPlayer.Script == null) return;

			//Wasn't so dead, let's kill them
			if (deadPlayer.Script.playerHealth != null &&
			    deadPlayer.Script.playerHealth.IsDead == false)
			{
				deadPlayer.Script.playerHealth.ApplyDamage(
					player,
					200,
					AttackType.Internal,
					DamageType.Brute);
			}

			TryRespawn(deadPlayer, msg, msg.OccupationToRespawn);
		}

		void TryRespawn(ConnectedPlayer deadPlayer, RequestRespawnPlayerNetMessage msg, string occupation = null)
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

		public static RequestRespawnPlayerNetMessage SendNormalRespawn(string userId, string adminToken, string userIDToRespawn,
			Occupation occupation)
		{
			var msg = new RequestRespawnPlayerNetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 0
			};

			new RequestRespawnPlayer().Send(msg);
			return msg;
		}

		public static RequestRespawnPlayerNetMessage SendSpecialRespawn(string userID, string adminToken,
			string userIDToRespawn,
			Occupation occupation)
		{
			var msg = new RequestRespawnPlayerNetMessage()
			{
				Userid = userID,
				AdminToken = adminToken,
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 1
			};

			new RequestRespawnPlayer().Send(msg);
			return msg;
		}

		public static RequestRespawnPlayerNetMessage SendAntagRespawn(string userID, string adminToken, string userIdTorespawn,
			Antagonist antagonist)
		{
			var msg = new RequestRespawnPlayerNetMessage()
			{
				Userid = userID,
				AdminToken = adminToken,
				UserToRespawn = userIdTorespawn,
				OccupationToRespawn = antagonist.AntagName,
				Type = 2
			};

			new RequestRespawnPlayer().Send(msg);
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
