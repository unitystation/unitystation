using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update temperature
/// </summary>
public class HealthTemperatureMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthTemperatureStats;

	public NetworkInstanceId EntityToUpdate;
	public float temperature;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientTemperatureStats(temperature);
	}

	public static HealthTemperatureMessage Send(GameObject recipient, GameObject entityToUpdate, float temperatureValue)
	{
		HealthTemperatureMessage msg = new HealthTemperatureMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			temperature = temperatureValue
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthTemperatureMessage SendToAll(GameObject entityToUpdate, float temperatureValue)
	{
		HealthTemperatureMessage msg = new HealthTemperatureMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			temperature = temperatureValue
		};
		msg.SendToAll();
		return msg;
	}
}
