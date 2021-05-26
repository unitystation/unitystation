using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update brain health stats
	/// </summary>
	public class HealthBrainMessage : ServerMessage<HealthBrainMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint EntityToUpdate;
			public bool IsHusk;
			public int BrainDamage;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.EntityToUpdate);
			if(NetworkObject != null) NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBrainStats(msg.IsHusk, msg.BrainDamage);
		}

		public static NetMessage Send(GameObject recipient, GameObject entityToUpdate, bool isHusk, int brainDamage)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsHusk = isHusk,
				BrainDamage = brainDamage

			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll(GameObject entityToUpdate, bool isHusk, int brainDamage)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsHusk = isHusk,
				BrainDamage = brainDamage
			};

			SendToAll(msg);
			return msg;
		}
	}
}
