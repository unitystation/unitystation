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
			if(msg.Target.TryGetComponent<LivingHealthMasterBase>(out var healthMaster) == false) return;
			healthMaster.TryDeafen(msg.DeafenValue, network: false);		
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newflashValue, Ears target)
		{
			NetMessage msg = new NetMessage
			{
				DeafenValue = newflashValue,
				Target = clientConn
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}