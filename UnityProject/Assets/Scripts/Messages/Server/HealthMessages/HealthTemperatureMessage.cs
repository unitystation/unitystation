using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update temperature
/// </summary>
public class HealthTemperatureMessage : ServerMessage
{
	public class HealthTemperatureMessageNetMessage : NetworkMessage
	{
		public float temperature;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as HealthTemperatureMessageNetMessage;
		if(newMsg == null) return;

		PlayerManager.LocalPlayerScript.playerHealth?.UpdateClientTemperatureStats(newMsg.temperature);
	}

	public static HealthTemperatureMessageNetMessage Send(GameObject entityToUpdate, float temperatureValue)
	{
		HealthTemperatureMessageNetMessage msg = new HealthTemperatureMessageNetMessage
		{
			temperature = temperatureValue
		};
		new HealthTemperatureMessage().SendTo(entityToUpdate, msg);
		return msg;
	}
}
