using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update pressure
/// </summary>
public class HealthPressureMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthPressureStats;

	public NetworkInstanceId EntityToUpdate;
	public float pressure;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientPressureStats(pressure);
	}

	public static HealthPressureMessage Send(GameObject entityToUpdate, float pressureValue)
	{
		HealthPressureMessage msg = new HealthPressureMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			pressure = pressureValue
		};
		msg.SendTo(entityToUpdate);
		return msg;
	}
}
