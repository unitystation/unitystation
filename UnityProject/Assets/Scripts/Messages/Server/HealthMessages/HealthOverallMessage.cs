using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update overall health stats and conscious state
/// </summary>
public class HealthOverallMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthOverallStats;

	public NetworkInstanceId EntityToUpdate;
	public int OverallHealth;
	public ConsciousState ConsciousState;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientHealthStats(OverallHealth, ConsciousState);
	}

	public static HealthOverallMessage Send(GameObject recipient, GameObject entityToUpdate, int overallHealth, ConsciousState consciousState)
	{
		HealthOverallMessage msg = new HealthOverallMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
				ConsciousState = consciousState
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthOverallMessage SendToAll(GameObject entityToUpdate, int overallHealth, ConsciousState consciousState)
	{
		HealthOverallMessage msg = new HealthOverallMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				OverallHealth = overallHealth,
				ConsciousState = consciousState
		};
		msg.SendToAll();
		return msg;
	}
}