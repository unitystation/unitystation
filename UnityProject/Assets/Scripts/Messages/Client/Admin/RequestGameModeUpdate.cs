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
			public string NextGameMode;
			public bool IsSecret;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				if (GameManager.Instance.NextGameMode != msg.NextGameMode)
				{
					Logger.Log(
							$"{SentByPlayer.Username} with uid: {SentByPlayer.UserId}, has updated the next game mode with {msg.NextGameMode}",
							Category.Admin);
					GameManager.Instance.NextGameMode = msg.NextGameMode;
				}

				if (GameManager.Instance.SecretGameMode != msg.IsSecret)
				{
					Logger.Log(
							$"{SentByPlayer.Username} with uid: {SentByPlayer.UserId}, has set the IsSecret GameMode flag to {msg.IsSecret}",
							Category.Admin);
					GameManager.Instance.SecretGameMode = msg.IsSecret;
				}
			}
		}

		public static NetMessage Send(string nextGameMode, bool isSecret)
		{
			NetMessage msg = new NetMessage
			{
				NextGameMode = nextGameMode,
				IsSecret = isSecret
			};

			Send(msg);
			return msg;
		}
	}
}
