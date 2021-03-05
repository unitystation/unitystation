using System.Collections;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	///     Message that tells client to add a log to their logger. This is for
	/// 	easy debugging for players
	/// </summary>
	public class SendClientLogMessage : ServerMessage<SendClientLogMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Message;
			public Category Category;
			public bool IsError;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.IsError)
			{
				Logger.LogError(msg.Message, msg.Category);
			}
			else
			{
				Logger.Log(msg.Message, msg.Category);
			}
		}

		public static NetMessage SendLogToClient(GameObject clientPlayer, string message, Category logCat,
			bool showError)
		{
			NetMessage msg = new NetMessage {Message = message, Category = logCat, IsError = showError};

			SendTo(clientPlayer, msg);
			return msg;
		}
	}
}