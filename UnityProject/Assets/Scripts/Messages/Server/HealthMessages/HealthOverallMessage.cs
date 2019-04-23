using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update overall health stats
/// </summary>
public class HealthOverallMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthOverallStats;

	public NetworkInstanceId EntityToUpdate;
	public float OverallHealth;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientHealthStats(OverallHealth);
	}

	public static HealthOverallMessage Send(GameObject recipient, GameObject entityToUpdate, float overallHealth)
	{
		HealthOverallMessage msg = new HealthOverallMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthOverallMessage SendToAll(GameObject entityToUpdate, float overallHealth)
	{
		HealthOverallMessage msg = new HealthOverallMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
		};
		msg.SendToAll();
		return msg;
	}
}