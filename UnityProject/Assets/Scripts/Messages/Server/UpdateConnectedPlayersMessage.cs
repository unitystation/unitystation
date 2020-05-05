using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;

/// <summary>
///     Message that tells clients what their ConnectedPlayers list should contain
/// </summary>
public class UpdateConnectedPlayersMessage : ServerMessage
{
	public ClientConnectedPlayer[] Players;

	public override void Process()
	{
//		Logger.Log("Processed " + ToString());
		if (PlayerList.Instance == null || PlayerList.Instance.ClientConnectedPlayers == null)
		{
			return;
		}

		if (Players != null)
		{
			Logger.LogFormat("This client got an updated PlayerList state: {0}", Category.Connections, string.Join(",", Players));
			PlayerList.Instance.ClientConnectedPlayers.Clear();
			for (var i = 0; i < Players.Length; i++)
			{
				PlayerList.Instance.ClientConnectedPlayers.Add(Players[i]);
			}
		}

		PlayerList.Instance.RefreshPlayerListText();
		UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().UpdateJobsList();
		UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().UpdatePlayerCount(Players?.Length ?? 0);
	}

	public static UpdateConnectedPlayersMessage Send()
	{
		Logger.LogFormat("This server informing all clients of the new PlayerList state: {0}", Category.Connections,
			string.Join(",", PlayerList.Instance.AllPlayers));
		UpdateConnectedPlayersMessage msg = new UpdateConnectedPlayersMessage();
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

			prepareConnectedPlayers.Add(new ClientConnectedPlayer
			{
				Name = c.Name,
				Job = c.Job,
				PendingSpawn = pendingSpawn
			});
		}

		msg.Players = prepareConnectedPlayers.ToArray();

		msg.SendToAll();
		return msg;
	}
}