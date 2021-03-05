using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	/// <summary>
	///     Request to change game mode settings (admin only)
	/// </summary>
	public class RequestGameModeUpdate : ClientMessage<RequestGameModeUpdate.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string NextGameMode;
			public bool IsSecret;
		}

		public override void Process(NetMessage msg)
		{
			var admin = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (admin != null)
			{
				if (GameManager.Instance.NextGameMode != msg.NextGameMode)
				{
					Logger.Log(admin.Player().Username + $" with uid: {msg.Userid}, has updated the next game mode with {msg.NextGameMode}", Category.Admin);
					GameManager.Instance.NextGameMode = msg.NextGameMode;
				}

				if (GameManager.Instance.SecretGameMode != msg.IsSecret)
				{
					Logger.Log(admin.Player().Username + $" with uid: {msg.Userid}, has set the IsSecret GameMode flag to {msg.IsSecret}", Category.Admin);
					GameManager.Instance.SecretGameMode = msg.IsSecret;
				}
			}
		}

		public static NetMessage Send(string userId, string adminToken, string nextGameMode, bool isSecret)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				NextGameMode = nextGameMode,
				IsSecret = isSecret
			};

			Send(msg);
			return msg;
		}
	}
}
