using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Experimental.XR;

/// <summary>
///     Message that tells clients what their ConnectedPlayers list should contain
/// </summary>
public class UpdateConnectedPlayersMessage : ServerMessage
{
	public class UpdateConnectedPlayersMessageNetMessage : NetworkMessage
	{
		public ClientConnectedPlayer[] Players;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as UpdateConnectedPlayersMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

//		Logger.Log("Processed " + ToString());
		if (PlayerList.Instance == null || PlayerList.Instance.ClientConnectedPlayers == null)
		{
			return;
		}

		if (newMsg.Players != null)
		{
			Logger.LogFormat("This client got an updated PlayerList state: {0}", Category.Connections, string.Join(",", newMsg.Players));
			PlayerList.Instance.ClientConnectedPlayers.Clear();
			for (var i = 0; i < newMsg.Players.Length; i++)
			{
				PlayerList.Instance.ClientConnectedPlayers.Add(newMsg.Players[i]);
			}
		}

		PlayerList.Instance.RefreshPlayerListText();
		UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
		UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().UpdatePlayerCount(newMsg.Players?.Length ?? 0);
	}

	public static UpdateConnectedPlayersMessageNetMessage Send()
	{
		Logger.LogFormat("This server informing all clients of the new PlayerList state: {0}", Category.Connections,
			string.Join(",", PlayerList.Instance.AllPlayers));
		UpdateConnectedPlayersMessageNetMessage msg = new UpdateConnectedPlayersMessageNetMessage();
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

		new UpdateConnectedPlayersMessage().SendToAll(msg);
		return msg;
	}
}