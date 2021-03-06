using Mirror;
using UnityEngine;

namespace Messages.Server
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