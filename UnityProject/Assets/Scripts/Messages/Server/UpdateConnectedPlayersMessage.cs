using System.Collections.Generic;
using Logs;
using Mirror;
using UI;

namespace Messages.Server
{
	/// <summary>
	///     Message that tells clients what their ConnectedPlayers list should contain
	/// </summary>
	public class UpdateConnectedPlayersMessage : ServerMessage<UpdateConnectedPlayersMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ClientConnectedPlayer[] Players;
		}

		public override void Process(NetMessage msg)
		{
			if (PlayerList.Instance == null || PlayerList.Instance.ClientConnectedPlayers == null) return;

			if (msg.Players != null)
			{
				Loggy.LogFormat("This client got an updated PlayerList state: {0}", Category.Connections, string.Join(",", msg.Players));
				PlayerList.Instance.ClientConnectedPlayers.Clear();
				for (var i = 0; i < msg.Players.Length; i++)
				{
					PlayerList.Instance.ClientConnectedPlayers.Add(msg.Players[i]);
				}
			}

			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
			UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().UpdatePlayerCount(msg.Players?.Length ?? 0);
		}

		public static NetMessage Send()
		{
			//Performance issue with string.Join doing this at high player count
			//If this is necessary in the future cache it when players leave/join?
			//Logger.LogFormat("This server informing all clients of the new PlayerList state: {0}", Category.Connections,
			//	string.Join(",", PlayerList.Instance.AllPlayers));

			var prepareConnectedPlayers = new List<ClientConnectedPlayer>();
			var count = 0;
			foreach (PlayerInfo c in PlayerList.Instance.AllPlayers)
			{
				var tag = "";

				if (PlayerList.Instance.IsAdmin(c.UserId))
				{
					tag = "<color=red>[Admin]</color>";
				}
				else if (PlayerList.Instance.IsMentor(c.UserId))
				{
					tag = "<color=#6400ff>[Mentor]</color>";
				}

				prepareConnectedPlayers.Add(new ClientConnectedPlayer
				{
					UserName = c.Username,
					Tag = tag,
					Index = count,
					PingToServer = (int?)(c.Script?.RTT * 1000) ?? -1
				});

				count++;
			}

			NetMessage msg = new NetMessage();
			msg.Players = prepareConnectedPlayers.ToArray();

			SendToAll(msg);
			return msg;
		}
	}
}
