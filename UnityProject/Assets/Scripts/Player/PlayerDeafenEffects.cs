using UnityEngine;
using Mirror;
using Messages.Server;
using Items.Implants.Organs;

namespace Player
{
	public class PlayerDeafenEffectsMessage : ServerMessage<PlayerDeafenEffectsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float DeafenValue;
			public Ears Target;
		}

		public override void Process(NetMessage msg)
		{
			msg.Target.StopAllCoroutines();
			msg.Target.TemporaryDeafen(msg.DeafenValue);		
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newflashValue, Ears target)
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