using System.Collections;
using Messages.Client;
using Mirror;
using UnityEngine;

/// <summary>
///     Request to change game mode settings (admin only)
/// </summary>
public class RequestGameModeUpdate : ClientMessage
{
	public struct RequestGameModeUpdateNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public string NextGameMode;
		public bool IsSecret;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestGameModeUpdateNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestGameModeUpdateNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		var admin = PlayerList.Instance.GetAdmin(newMsg.Userid, newMsg.AdminToken);
		if (admin != null)
		{
			if (GameManager.Instance.NextGameMode != newMsg.NextGameMode)
			{
				Logger.Log(admin.Player().Username + $" with uid: {newMsg.Userid}, has updated the next game mode with {newMsg.NextGameMode}", Category.Admin);
				GameManager.Instance.NextGameMode = newMsg.NextGameMode;
			}

			if (GameManager.Instance.SecretGameMode != newMsg.IsSecret)
			{
				Logger.Log(admin.Player().Username + $" with uid: {newMsg.Userid}, has set the IsSecret GameMode flag to {newMsg.IsSecret}", Category.Admin);
				GameManager.Instance.SecretGameMode = newMsg.IsSecret;
			}
		}
	}

	public static RequestGameModeUpdateNetMessage Send(string userId, string adminToken, string nextGameMode, bool isSecret)
	{
		RequestGameModeUpdateNetMessage msg = new RequestGameModeUpdateNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			NextGameMode = nextGameMode,
			IsSecret = isSecret
		};

		new RequestGameModeUpdate().Send(msg);
		return msg;
	}
}
