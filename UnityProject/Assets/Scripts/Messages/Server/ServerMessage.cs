using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;
using Util;

namespace Messages.Server
{
	/// <summary>
	///     Represents a network message sent from the server to clients.
	///     Sending a message will invoke the Process() method on the client.
	///
	///		Channel 0 reliable, 1 unreliable, 2 reliable(For Chat Messages) and 3 reliable(For VV to not cause blockages)
	/// </summary>
	public abstract class ServerMessage<T> : GameMessageBase<T> where T : struct, NetworkMessage
	{
		public static void SendToAll(T msg, int channel = 0)
		{
			NetworkServer.SendToAll(msg, channel, sendToReadyOnly: true);
			Loggy.LogTraceFormat("SentToAll {0}", Category.Server, msg.GetType());
		}

		public static void SendToAllExcept(T msg, GameObject excluded, int channel = 0)
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
					connection.Value.Send(msg, channel);
				}
			}

			Loggy.LogTraceFormat("SentToAllExcept {1}: {0}", Category.Server, msg.GetType(), excluded.name);
		}

		public static void SendTo(GameObject recipient, T msg, Category category = Category.Server, int channel = 0)
		{
			if (recipient == null)
			{
				return;
			}

			NetworkConnection connection = recipient.GetComponent<NetworkIdentity>().connectionToClient;

			if (connection == null)
			{
				connection = PlayerList.Instance.GetRelatedNetworkConnection(recipient);
				if (connection == null) return;
			}

			//only send to players that are currently controlled by a client
			if (PlayerList.Instance.Has(connection))
			{
				connection.Send(msg, channel);
				Loggy.LogTraceFormat("SentTo {0}: {1}", category, recipient.name, msg.GetType());
			}
			else
			{
				Loggy.LogTraceFormat("Not sending message {0} to {1}", category, msg.GetType(), recipient.name);
			}
		}

		public static void SendTo(PlayerInfo recipient, T msg, int channel = 0)
		{
			if (recipient == null) return;
			SendTo(recipient.Connection, msg, channel);
		}

		public static void SendTo(NetworkConnection recipient, T msg, int channel = 0)
		{
			if (recipient == null) return;
			recipient.Send(msg, channel);
		}

		/// <summary>
		/// Sends the network message only to players who are visible from the
		/// worldPosition
		/// </summary>
		public static void SendToVisiblePlayers(Vector2 worldPosition, T msg, int channel = 0, bool doLinecast = true)
		{
			var players = OtherUtil.GetVisiblePlayers(worldPosition, doLinecast);

			//Sends the message only to visible players:
			foreach (PlayerInfo player in players)
			{
				if (player.Script.netIdentity == null) continue;

				if (PlayerList.Instance.Has(player.Connection))
				{
					player.Connection.Send(msg, channel);
				}
			}
		}

		/// <summary>
		/// Sends the network message only to players who are within a 15 tile radius
		/// of the worldPosition. This method disregards if the player is visible or not
		/// </summary>
		public static void SendToNearbyPlayers(Vector2 worldPosition, T msg, int channel = 0)
		{
			var players = PlayerList.Instance.InGamePlayers;

			for (int i = players.Count - 1; i >= 0; i--)
			{
				if (Vector2.Distance(worldPosition, players[i].GameObject.transform.position) > 15f) continue;

				players[i].Connection.Send(msg, channel);
			}
		}

		public static void SendToAdmins(T msg, int channel = 0)
		{
			var admins = PlayerList.Instance.GetAllAdmins();

			foreach (var admin in admins)
			{
				if (PlayerList.Instance.Has(admin.Connection))
				{
					admin.Connection.Send(msg, channel);
				}
			}
		}

		public static void SendToMentors(T msg, int channel = 0)
		{
			var mentors = PlayerList.Instance.GetAllMentors();

			foreach (var mentor in mentors)
			{
				if (PlayerList.Instance.Has(mentor.Connection))
				{
					mentor.Connection.Send(msg, channel);
				}
			}
			var admins = PlayerList.Instance.GetAllAdmins();

			foreach (var admin in admins)
			{
				if (PlayerList.Instance.Has(admin.Connection))
				{
					admin.Connection.Send(msg, channel);
				}
			}
		}
	}
}
