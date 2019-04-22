using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update blood health stats
/// </summary>
public class HealthBloodMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthBloodStats;

	public NetworkInstanceId EntityToUpdate;
	public int HeartRate;
	public int BloodLevel;
	public float OxygenDamage;
	public float ToxinLevel;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBloodStats(HeartRate, BloodLevel, OxygenDamage, ToxinLevel);
	}

	public static HealthBloodMessage Send(GameObject recipient, GameObject entityToUpdate, int heartRate, int bloodLevel,
		float oxygenDamage, float toxinLevel)
	{
		HealthBloodMessage msg = new HealthBloodMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinLevel = toxinLevel
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthBloodMessage SendToAll(GameObject entityToUpdate, int heartRate, int bloodLevel,
		float oxygenDamage, float toxinLevel)
	{
		HealthBloodMessage msg = new HealthBloodMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinLevel = toxinLevel
		};
		msg.SendToAll();
		return msg;
	}
}