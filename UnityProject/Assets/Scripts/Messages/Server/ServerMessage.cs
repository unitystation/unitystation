using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	///     Represents a network message sent from the server to clients.
	///     Sending a message will invoke the Process() method on the client.
	/// </summary>
	public abstract class ServerMessage<T> : GameMessageBase<T> where T : struct, NetworkMessage
	{
		public static void SendToAll(T msg)
		{
			NetworkServer.SendToAll(msg, 0);
			Logger.LogTraceFormat("SentToAll {0}", Category.Server, msg.GetType());
		}

		public static void SendToAllExcept(T msg, GameObject excluded)
		{
			if (excluded == null)
			{
				SendToAll(msg);
				return;
			}

			var excludedConnection = excluded.GetComponent<NetworkIdentity>().connectionToClient;

			foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
			{
				if (connection.Value != null && connection.Value != excludedConnection)
				{
					connection.Value.Send(msg, 0);
				}
			}

			Logger.LogTraceFormat("SentToAllExcept {1}: {0}", Category.Server, msg.GetType(), excluded.name);
		}

		public static void SendTo(GameObject recipient, T msg, Category category = Category.Server)
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

			//only send to players that are currently controlled by a client
			if (PlayerList.Instance.ContainsConnection(connection))
			{
				connection.Send(msg, 0);
				Logger.LogTraceFormat("SentTo {0}: {1}", category, recipient.name, msg.GetType());
			}
			else
			{
				Logger.LogTraceFormat("Not sending message {0} to {1}", category, msg.GetType(), recipient.name);
			}
		}

		public static void SendTo(ConnectedPlayer recipient, T msg)
		{
			if (recipient == null) return;
			SendTo(recipient.Connection, msg);
		}

		public static void SendTo(NetworkConnection recipient, T msg)
		{
			if (recipient == null) return;
			recipient.Send(msg, 0);
		}

		/// <summary>
		/// Sends the network message only to players who are visible from the
		/// worldPosition
		/// </summary>
		public static void SendToVisiblePlayers(Vector2 worldPosition, T msg)
		{
			var players = PlayerList.Instance.AllPlayers;

			LayerMask layerMask = LayerMask.GetMask( "Door Closed");
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
					if (MatrixManager.Linecast(worldPosition, LayerTypeSelection.Walls, layerMask,
						players[i].GameObject.transform.position).ItHit)
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
					player.Connection.Send(msg, 0);
				}
			}
		}

		/// <summary>
		/// Sends the network message only to players who are within a 15 tile radius
		/// of the worldPostion. This method disregards if the player is visible or not
		/// </summary>
		public static void SendToNearbyPlayers(Vector2 worldPosition, T msg)
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
					player.Connection.Send(msg, 0);
				}
			}
		}

		public static void SendToAdmins(T msg)
		{
			var admins = PlayerList.Instance.GetAllAdmins();

			foreach (var admin in admins)
			{
				if (PlayerList.Instance.ContainsConnection(admin.Connection))
				{
					admin.Connection.Send(msg, 0);
				}
			}
		}

		public static void SendToMentors(T msg)
		{
			var mentors = PlayerList.Instance.GetAllMentors();

			foreach (var mentor in mentors)
			{
				if (PlayerList.Instance.ContainsConnection(mentor.Connection))
				{
					mentor.Connection.Send(msg, 0);
				}
			}
			var admins = PlayerList.Instance.GetAllAdmins();

			foreach (var admin in admins)
			{
				if (PlayerList.Instance.ContainsConnection(admin.Connection))
				{
					admin.Connection.Send(msg, 0);
				}
			}
		}
	}
}
