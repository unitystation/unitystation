using UnityEngine;
using System.Collections.Generic;
using Mirror;

/// <summary>
///     Represents a network message sent from the server to clients.
///     Sending a message will invoke the Process() method on the client.
/// </summary>
public abstract class ServerMessage : GameMessageBase
{
	public void SendToAll()
	{
		NetworkServer.SendToAll(this, 0);
		Logger.LogTraceFormat("SentToAll {0}", Category.NetMessage, this);
	}

	public void SendToAllExcept(GameObject excluded)
	{
		if (excluded == null)
		{
			SendToAll();
			return;
		}

		var excludedConnection = excluded.GetComponent<NetworkIdentity>().connectionToClient;

		foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
		{
			if (connection.Value != null && connection.Value != excludedConnection)
			{
				connection.Value.Send(this, 0);
			}
		}

		Logger.LogTraceFormat("SentToAllExcept {1}: {0}", Category.NetMessage, this, excluded.name);
	}

	public void SendTo(GameObject recipient)
	{
		if (recipient == null)
		{
			return;
		}

		NetworkConnection connection = recipient.GetComponent<NetworkIdentity>().connectionToClient;

		if (connection == null)
		{
			return;
		}

//			only send to players that are currently controlled by a client
		if (PlayerList.Instance.ContainsConnection(connection))
		{
			connection.Send(this, 0);
			Logger.LogTraceFormat("SentTo {0}: {1}", Category.NetMessage, recipient.name, this);
		}
		else
		{
			Logger.LogTraceFormat("Not sending message {0} to {1}", Category.NetMessage, this, recipient.name);
		}

		//Obsolete version:
		//NetworkServer.SendToClientOfPlayer(recipient, GetMessageType(), this);
	}

	public void SendTo(NetworkConnection recipient)
	{
		if (recipient == null) return;
		recipient.Send(this, 0);
	}

	/// <summary>
	/// Sends the network message only to players who are visible from the
	/// worldPosition
	/// </summary>
	public void SendToVisiblePlayers(Vector2 worldPosition)
	{
		var players = PlayerList.Instance.AllPlayers;

		LayerMask layerMask = LayerMask.GetMask("Walls", "Door Closed");
		for (int i = players.Count - 1; i > 0; i--)
		{
			if (Vector2.Distance(worldPosition,
				    players[i].GameObject.transform.position) > 14f)
			{
				//Player in the list is too far away for this message, remove them:
				players.Remove(players[i]);
			}
			else
			{
				//within range, but check if they are in another room or hiding behind a wall
				if (Physics2D.Linecast(worldPosition,
					players[i].GameObject.transform.position, layerMask))
				{
					//if it hit a wall remove that player
					players.Remove(players[i]);
				}
			}
		}

		//Sends the message only to visible players:
		foreach (ConnectedPlayer player in players)
		{
			if (player == null || player.Script == null || player.Script.netIdentity == null) continue;

			if (PlayerList.Instance.ContainsConnection(player.Connection))
			{
				player.Connection.Send(this, 0);
			}
		}
	}

	/// <summary>
	/// Sends the network message only to players who are within a 15 tile radius
	/// of the worldPostion. This method disregards if the player is visible or not
	/// </summary>
	public void SendToNearbyPlayers(Vector2 worldPosition)
	{
		var players = PlayerList.Instance.AllPlayers;

		for (int i = players.Count - 1; i > 0; i--)
		{
			if (Vector2.Distance(worldPosition,
				    players[i].GameObject.transform.position) > 15f)
			{
				//Player in the list is too far away for this message, remove them:
				players.Remove(players[i]);
			}
		}

		foreach (ConnectedPlayer player in players)
		{
			if (player.Script == null) continue;

			if (PlayerList.Instance.ContainsConnection(player.Connection))
			{
				player.Connection.Send(this, 0);
			}
		}
	}

	public void SendToAdmins()
	{
		var admins = PlayerList.Instance.GetAllAdmins();

		foreach (var admin in admins)
		{
			if (PlayerList.Instance.ContainsConnection(admin.Connection))
			{
				admin.Connection.Send(this, 0);
			}
		}
	}
}