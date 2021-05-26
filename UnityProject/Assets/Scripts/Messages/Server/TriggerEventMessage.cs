using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Message that allows the server to broadcast an event to clients.
	/// </summary>
	public class TriggerEventMessage : ServerMessage<TriggerEventMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public Event EventType;
		}

		public override void Process(NetMessage msg)
		{
			EventManager.Broadcast(msg.EventType);
		}

		/// <summary>
		/// Send the event message to a specific player.
		/// </summary>
		public static NetMessage SendTo(GameObject recipient, Event eventType)
		{
			var msg = CreateMessage(eventType);

			SendTo(recipient, msg);
			return msg;
		}

		/// <summary>
		/// Send the event message to all players.
		/// </summary>
		public static NetMessage SendToAll(Event eventType)
		{
			var msg = CreateMessage(eventType);

			SendToAll(msg);
			return msg;
		}

		private static NetMessage CreateMessage(Event eventType)
		{
			return new NetMessage
			{
				EventType = eventType,
			};
		}
	}
}
