using System.Collections;
using UnityEngine;

/// <summary>
///     Message that tells client to add a log to their logger. This is for
/// 	easy debugging for players
/// </summary>
public class SendClientLogMessage : ServerMessage
{
	public string Message;
	public Category Category;
	public bool IsError;

	public override void Process()
	{
		if (IsError)
		{
			Logger.LogError(Message, Category);
		}
		else
		{
			Logger.Log(Message, Category);
		}
	}

	public static SendClientLogMessage SendLogToClient(GameObject clientPlayer, string message, Category logCat,
		bool showError)
	{
		SendClientLogMessage msg = new SendClientLogMessage {Message = message, Category = logCat, IsError = showError};

		msg.SendTo(clientPlayer);

		return msg;
	}
}