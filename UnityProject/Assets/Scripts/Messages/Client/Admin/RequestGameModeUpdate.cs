using System.Collections;
using Messages.Client;
using UnityEngine;

/// <summary>
///     Request to change game mode settings (admin only)
/// </summary>
public class RequestGameModeUpdate : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public string NextGameMode;
	public bool IsSecret;

	public override void Process()
	{
		var admin = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (admin != null)
		{
			if (GameManager.Instance.NextGameMode != NextGameMode)
			{
				Logger.Log(admin.Player().Username + $" with uid: {Userid}, has updated the next game mode with {NextGameMode}", Category.Admin);
				GameManager.Instance.NextGameMode = NextGameMode;
			}

			if (GameManager.Instance.SecretGameMode != IsSecret)
			{
				Logger.Log(admin.Player().Username + $" with uid: {Userid}, has set the IsSecret GameMode flag to {IsSecret}", Category.Admin);
				GameManager.Instance.SecretGameMode = IsSecret;
			}
		}
	}

	public static RequestGameModeUpdate Send(string userId, string adminToken, string nextGameMode, bool isSecret)
	{
		RequestGameModeUpdate msg = new RequestGameModeUpdate
		{
			Userid = userId,
			AdminToken = adminToken,
			NextGameMode = nextGameMode,
			IsSecret = isSecret
		};
		msg.Send();
		return msg;
	}
}
