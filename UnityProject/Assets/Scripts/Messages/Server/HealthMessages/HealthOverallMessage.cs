using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update overall health stats
	/// </summary>
	public class HealthOverallMessage : ServerMessage<HealthOverallMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint EntityToUpdate;
			public float OverallHealth;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.EntityToUpdate);
			NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientHealthStats(msg.OverallHealth);
		}

		public static NetMessage Send(GameObject recipient, GameObject entityToUpdate, float overallHealth)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll(GameObject entityToUpdate, float overallHealth)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
			};

			SendToAll(msg);
			return msg;
		}
	}
}