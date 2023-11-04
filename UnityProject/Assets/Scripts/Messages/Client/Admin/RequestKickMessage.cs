using Logs;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestKickMessage : ClientMessage<RequestKickMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string UserIDToKick;
			public string Reason;
			public bool Announce;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin() == false)
			{
				Loggy.Log($"Player {SentByPlayer.Username} tried to kick someone but they weren't an admin!", Category.Exploits);
			}

			if (PlayerList.Instance.TryGetByUserID(msg.UserIDToKick, out var player))
			{
				PlayerList.Instance.ServerKickPlayer(player, msg.Reason, msg.Announce);
				Loggy.Log($"Admin {SentByPlayer.Username} has kicked {player.Username}.", Category.Admin);
			}
		}

		public static NetMessage Send(string userIdToKick, string reason, bool announce = true)
		{
			NetMessage msg = new()
			{
				UserIDToKick = userIdToKick,
				Reason = reason,
				Announce = announce,
			};

			Send(msg);
			return msg;
		}
	}

	public class RequestBanMessage : ClientMessage<RequestBanMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string UserIDToBan;
			public string Reason;
			public bool Announce;
			public int Minutes;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin() == false)
			{
				Loggy.Log($"Player {SentByPlayer.Username} tried to ban someone but they weren't an admin!", Category.Exploits);
			}

			if (PlayerList.Instance.TryGetByUserID(msg.UserIDToBan, out var player))
			{
				PlayerList.Instance.ServerBanPlayer(player, msg.Reason, msg.Announce, msg.Minutes);
				Loggy.Log($"Admin {SentByPlayer.Username} has banned {player.Username}.", Category.Admin);
			}
		}

		public static NetMessage Send(string userIdToBan, string reason, bool announceBan = true, int minutes = 0)
		{
			NetMessage msg = new()
			{
				UserIDToBan = userIdToBan,
				Reason = reason,
				Announce = announceBan,
				Minutes = minutes,
			};

			Send(msg);
			return msg;
		}
	}
}
