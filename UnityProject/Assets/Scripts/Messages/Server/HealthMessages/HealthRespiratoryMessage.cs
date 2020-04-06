using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update respiratory health stats
/// </summary>
public class HealthRespiratoryMessage : ServerMessage
{
	public bool IsSuffocating;

	public override void Process()
	{
		PlayerManager.LocalPlayerScript.playerHealth?.UpdateClientRespiratoryStats(IsSuffocating);
	}

	public static HealthRespiratoryMessage Send(GameObject entityToUpdate, bool IsSuffocating)
	{
		HealthRespiratoryMessage msg = new HealthRespiratoryMessage
		{
			IsSuffocating = IsSuffocating
		};
		msg.SendTo(entityToUpdate);
		return msg;
	}
}
