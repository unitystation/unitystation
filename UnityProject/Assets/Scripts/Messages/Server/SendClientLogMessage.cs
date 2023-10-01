using Logs;
using Mirror;

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
				Loggy.LogError(msg.Message, msg.Category);
			}
			else
			{
				Loggy.Log(msg.Message, msg.Category);
			}
		}

		public static NetMessage SendLogToClient(PlayerInfo player, string message, Category category = Category.Unknown)
		{
			var msg = new NetMessage
			{
				Message = message,
				Category = category,
				IsError = false,
			};

			SendTo(player, msg);
			return msg;
		}

		public static NetMessage SendErrorToClient(PlayerInfo player, string message, Category category = Category.Unknown)
		{
			var msg = new NetMessage
			{
				Message = message,
				Category = category,
				IsError = true,
			};

			SendTo(player, msg);
			return msg;
		}
	}
}
