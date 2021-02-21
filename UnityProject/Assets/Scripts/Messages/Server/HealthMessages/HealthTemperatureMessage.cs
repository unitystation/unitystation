using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update temperature
/// </summary>
public class HealthTemperatureMessage : ServerMessage
{
	public struct HealthTemperatureMessageNetMessage : NetworkMessage
	{
		public float temperature;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public HealthTemperatureMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as HealthTemperatureMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
