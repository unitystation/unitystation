using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update pressure
/// </summary>
public class HealthPressureMessage : ServerMessage
{
	public class HealthPressureMessageNetMessage : NetworkMessage
	{
		public float pressure;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as HealthPressureMessageNetMessage;
		if(newMsg == null) return;

		PlayerManager.LocalPlayerScript.playerHealth?.UpdateClientPressureStats(newMsg.pressure);
	}

	public static HealthPressureMessageNetMessage Send(GameObject entityToUpdate, float pressureValue)
	{
		HealthPressureMessageNetMessage msg = new HealthPressureMessageNetMessage
		{
			pressure = pressureValue
		};
		new HealthPressureMessage().SendTo(entityToUpdate, msg);
		return msg;
	}
}
