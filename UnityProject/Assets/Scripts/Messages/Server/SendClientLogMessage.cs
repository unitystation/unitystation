using System.Collections;
using UnityEngine;

/// <summary>
///     Message that tells client to add a log to their logger. This is for
/// 	easy debugging for players
/// </summary>
public class SendClientLogMessage : ServerMessage
{
	public class SendClientLogMessageNetMessage : ActualMessage
	{
		public string Message;
		public Category Category;
		public bool IsError;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as SendClientLogMessageNetMessage;
		if(newMsg == null) return;

		if (newMsg.IsError)
		{
			Logger.LogError(newMsg.Message, newMsg.Category);
		}
		else
		{
			Logger.Log(newMsg.Message, newMsg.Category);
		}
	}

	public static SendClientLogMessageNetMessage SendLogToClient(GameObject clientPlayer, string message, Category logCat,
		bool showError)
	{
		SendClientLogMessageNetMessage msg = new SendClientLogMessageNetMessage {Message = message, Category = logCat, IsError = showError};

		new SendClientLogMessage().SendTo(clientPlayer, msg);

		return msg;
	}
}