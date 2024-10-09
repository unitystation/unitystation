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
			public NetworkIdentity Target;
		}

		public override void Process(NetMessage msg)
		{
			if(msg.Target.gameObject.TryGetComponent<Ears>(out var earsToEffect) == false) return;
			earsToEffect.StopAllCoroutines();
			earsToEffect.TemporaryDeafen(msg.DeafenValue);
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newflashValue, NetworkIdentity target)
		{
			NetMessage msg = new NetMessage
			{
				DeafenValue = newflashValue,
				Target = target
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}