using Mirror;
using UnityEngine;

namespace Messages.Server.HealthMessages
{
	/// <summary>
	///     Tells client to update blood health stats
	/// </summary>
	public class HealthBloodMessage : ServerMessage<HealthBloodMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint EntityToUpdate;
			public int HeartRate;
			public float BloodLevel;
			public float OxygenDamage;
			public float ToxinLevel;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.EntityToUpdate);

			if (NetworkObject == null)
			{
				Debug.LogError("Couldn't load player gameobject for HealthBloodMessage");
				return;
			}

			NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBloodStats(msg.HeartRate,
				msg.BloodLevel, msg.OxygenDamage, msg.ToxinLevel);
		}

		public static NetMessage Send(GameObject recipient, GameObject entityToUpdate, int heartRate, float bloodLevel,
			float oxygenDamage, float toxinLevel)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinLevel = toxinLevel
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll(GameObject entityToUpdate, int heartRate, float bloodLevel,
			float oxygenDamage, float toxinLevel)
		{
			NetMessage msg = new NetMessage
			{
				EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinLevel = toxinLevel
			};

			SendToAll(msg);
			return msg;
		}
	}
}