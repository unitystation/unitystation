using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update blood health stats
/// </summary>
public class HealthBloodMessage : ServerMessage
{
	public class HealthBloodMessageNetMessage : ActualMessage
	{
		public uint EntityToUpdate;
		public int HeartRate;
		public float BloodLevel;
		public float OxygenDamage;
		public float ToxinLevel;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as HealthBloodMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBloodStats(newMsg.HeartRate,
			newMsg.BloodLevel, newMsg.OxygenDamage, newMsg.ToxinLevel);
	}

	public static HealthBloodMessageNetMessage Send(GameObject recipient, GameObject entityToUpdate, int heartRate, float bloodLevel,
		float oxygenDamage, float toxinLevel)
	{
		HealthBloodMessageNetMessage msg = new HealthBloodMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinLevel = toxinLevel
		};

		new HealthBloodMessage().SendTo(recipient, msg);
		return msg;
	}

	public static HealthBloodMessageNetMessage SendToAll(GameObject entityToUpdate, int heartRate, float bloodLevel,
		float oxygenDamage, float toxinLevel)
	{
		HealthBloodMessageNetMessage msg = new HealthBloodMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinLevel = toxinLevel
		};

		new HealthBloodMessage().SendToAll(msg);
		return msg;
	}
}