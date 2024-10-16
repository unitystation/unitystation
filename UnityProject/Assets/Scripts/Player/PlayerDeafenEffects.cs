using UnityEngine;
using Mirror;
using Messages.Server;
using Items.Implants.Organs;
using HealthV2;

namespace Player
{
	public class PlayerDeafenEffectsMessage : ServerMessage<PlayerDeafenEffectsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float DeafenValue;
			public GameObject Target;
		}

		public override void Process(NetMessage msg)
		{
			if(msg.Target.TryGetComponent<Ears>(out var earsToEffect) == false) return;
			earsToEffect.StopAllCoroutines();
			earsToEffect.DeafenFromMsg(msg.DeafenValue);
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newDeafenValue, GameObject target)
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