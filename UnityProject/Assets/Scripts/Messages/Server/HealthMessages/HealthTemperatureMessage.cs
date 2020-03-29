using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update temperature
/// </summary>
public class HealthTemperatureMessage : ServerMessage
{
	public override short MessageType => (short)MessageTypes.HealthTemperatureStats;
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
