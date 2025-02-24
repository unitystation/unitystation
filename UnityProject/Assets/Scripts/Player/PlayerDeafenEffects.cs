using System;
using UnityEngine;
using Mirror;
using Messages.Server;
using Items.Implants.Organs;
using HealthV2;
using Logs;

namespace Player
{
	public class PlayerDeafenEffectsMessage : ServerMessage<PlayerDeafenEffectsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float DeafenValue;
			public uint Target;
		}

		public override void Process(NetMessage msg)
		{
			if (NetworkClient.spawned.TryGetValue(msg.Target, out NetworkIdentity identity) == false)
			{
				Loggy.Warning($"Attempted to deafen {msg.Target} but it doesn't exist.");
				return;
			}
			GameObject targetObject = identity.gameObject;

			if (targetObject.TryGetComponent<Ears>(out var earsToEffect) == false)
			{
				Loggy.Warning($"Attempted to deafen {targetObject.name}, but no Ear component was attached.");
				return;
			}

			earsToEffect.StopAllCoroutines();
			earsToEffect.DeafenFromMsg(msg.DeafenValue);
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newDeafenValue, uint target)
		{
			NetMessage msg = new NetMessage
			{
				DeafenValue = newDeafenValue,
				Target = target
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}