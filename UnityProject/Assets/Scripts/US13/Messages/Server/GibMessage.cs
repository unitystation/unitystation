using Mirror;
using UnityEngine;
using US13.Health.Living;

namespace US13.Messages.Server
{
	public class GibMessage : ServerMessage<GibMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			foreach (LivingHealthBehaviour living in Object.FindObjectsOfType<LivingHealthBehaviour>())
			{
				living.Death();
			}
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			SendToAll(msg);
			return msg;
		}
	}
}