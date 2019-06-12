using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update pressure
/// </summary>
public class HealthPressureMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthPressureStats;
	public float pressure;

	public override IEnumerator Process()
	{
		yield return null;
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
