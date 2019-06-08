using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update temperature
/// </summary>
public class HealthTemperatureMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthTemperatureStats;
	public float temperature;

	public override IEnumerator Process()
	{
		yield return null;
		PlayerManager.LocalPlayerScript.playerHealth?.UpdateClientTemperatureStats(temperature);
	}

	public static HealthTemperatureMessage Send(GameObject entityToUpdate, float temperatureValue)
	{
		HealthTemperatureMessage msg = new HealthTemperatureMessage
		{
			temperature = temperatureValue
		};
		msg.SendTo(entityToUpdate);
		return msg;
	}
}
