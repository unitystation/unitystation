using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update pressure
/// </summary>
public class HealthPressureMessage : ServerMessage
{
	public float pressure;

	public override void Process()
	{
		PlayerManager.LocalPlayerScript.playerHealth?.UpdateClientPressureStats(pressure);
	}

	public static HealthPressureMessage Send(GameObject entityToUpdate, float pressureValue)
	{
		HealthPressureMessage msg = new HealthPressureMessage
		{
			pressure = pressureValue
		};
		msg.SendTo(entityToUpdate);
		return msg;
	}
}
