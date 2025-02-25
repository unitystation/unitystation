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
		}

		public override void Process(NetMessage msg)
		{
			var health = PlayerManager.LocalPlayerScript.playerHealth;
			if (health == null) return;

			var ears = health.GetBodyPartsInArea(BodyPartType.Ears, false);
			foreach (var ear in ears)
			{
				var earScript = ear.GetComponentCustom<Ears>();
				if (earScript == null) continue;

				earScript.StopAllCoroutines();
				earScript.DeafenFromMsg(msg.DeafenValue);
			}
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newDeafenValue)
		{
			NetMessage msg = new NetMessage
			{
				DeafenValue = newDeafenValue,
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}