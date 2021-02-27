using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update body part health stats
	/// </summary>
	public class HealthBodyPartMessage : ServerMessage<HealthBodyPartMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint EntityToUpdate;
			public BodyPartType BodyPart;
			public float BruteDamage;
			public float BurnDamage;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.EntityToUpdate);
			if (NetworkObject != null){
				NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBodyPartStats(msg.BodyPart, msg.BruteDamage, msg.BurnDamage);
			}
		}

		public static NetMessage Send(GameObject recipient, GameObject entityToUpdate, BodyPartType bodyPartType,
			float bruteDamage, float burnDamage)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				BodyPart = bodyPartType,
				BruteDamage = bruteDamage,
				BurnDamage = burnDamage

			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll(GameObject entityToUpdate, BodyPartType bodyPartType,
			float bruteDamage, float burnDamage)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				BodyPart = bodyPartType,
				BruteDamage = bruteDamage,
				BurnDamage = burnDamage
			};

			SendToAll(msg);
			return msg;
		}
	}
}