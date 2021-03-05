﻿using System.Collections.Generic;
using Mirror;

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
			//Logger.Log("Processed " + ToString());
			if (PlayerList.Instance == null || PlayerList.Instance.ClientConnectedPlayers == null)
			{
				return;
			}

			if (msg.Players != null)
			{
				Logger.LogFormat("This client got an updated PlayerList state: {0}", Category.Connections, string.Join(",", msg.Players));
				PlayerList.Instance.ClientConnectedPlayers.Clear();
				for (var i = 0; i < msg.Players.Length; i++)
				{
					PlayerList.Instance.ClientConnectedPlayers.Add(msg.Players[i]);
				}
			}

			PlayerList.Instance.RefreshPlayerListText();
			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
			UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().UpdatePlayerCount(msg.Players?.Length ?? 0);
		}

		public static NetMessage Send()
		{
			Logger.LogFormat("This server informing all clients of the new PlayerList state: {0}", Category.Connections,
				string.Join(",", PlayerList.Instance.AllPlayers));
			NetMessage msg = new NetMessage();
			var prepareConnectedPlayers = new List<ClientConnectedPlayer>();
			bool pendingSpawn = false;
			foreach (ConnectedPlayer c in PlayerList.Instance.AllPlayers)
			{
				if(c.Connection == null) continue; //offline player

				if (string.IsNullOrEmpty(c.Name))
				{
					if (c.GameObject != null)
					{
						var joinedViewer = c.GameObject.GetComponent<JoinedViewer>();
						if (joinedViewer != null)
						{
							pendingSpawn = true;
						}
						else
						{
							continue;
						}
					}
					else
					{
						continue;
					}
				}

				var tag = "";

				if (PlayerList.Instance.IsAdmin(c.UserId))
				{
					tag = "<color=red>[Admin]</color>";
				} else if (PlayerList.Instance.IsMentor(c.UserId))
				{
					tag = "<color=#6400ff>[Mentor]</color>";
				}

				prepareConnectedPlayers.Add(new ClientConnectedPlayer
				{
					UserName = c.Username,
					Name = c.Name,
					Job = c.Job,
					PendingSpawn = pendingSpawn,
					Tag = tag
				});
			}

			msg.Players = prepareConnectedPlayers.ToArray();

			SendToAll(msg);
			return msg;
		}
	}
}