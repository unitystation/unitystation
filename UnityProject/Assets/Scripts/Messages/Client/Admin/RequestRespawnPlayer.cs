using Antagonists;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestRespawnPlayer : ClientMessage
	{
		public string Userid;
		public string AdminToken;
		public string UserToRespawn;
		public string OccupationToRespawn;
		public int Type;

		public override void Process()
		{
			VerifyAdminStatus();
		}

		void VerifyAdminStatus()
		{
			var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
			if (player == null) return;
			var deadPlayer = PlayerList.Instance.GetByUserID(UserToRespawn);
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

			TryRespawn(deadPlayer, OccupationToRespawn);
		}

		void TryRespawn(ConnectedPlayer deadPlayer, string occupation = null)
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{PlayerList.Instance.GetByUserID(Userid).Name} respawned dead player {deadPlayer.Name} as {occupation}", Userid);

			var respawnType = (RespawnType) Type;

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

		public static RequestRespawnPlayer SendNormalRespawn(string userId, string adminToken, string userIDToRespawn,
			Occupation occupation)
		{
			var msg = new RequestRespawnPlayer
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 0
			};

			msg.Send();
			return msg;
		}

		public static RequestRespawnPlayer SendSpecialRespawn(string userID, string adminToken,
			string userIDToRespawn,
			Occupation occupation)
		{
			var msg = new RequestRespawnPlayer()
			{
				Userid = userID,
				AdminToken = adminToken,
				UserToRespawn = userIDToRespawn,
				OccupationToRespawn = occupation.name,
				Type = 1
			};

			msg.Send();
			return msg;
		}

		public static RequestRespawnPlayer SendAntagRespawn(string userID, string adminToken, string userIdTorespawn,
			Antagonist antagonist)
		{
			var msg = new RequestRespawnPlayer()
			{
				Userid = userID,
				AdminToken = adminToken,
				UserToRespawn = userIdTorespawn,
				OccupationToRespawn = antagonist.AntagName,
				Type = 2
			};

			msg.Send();
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
