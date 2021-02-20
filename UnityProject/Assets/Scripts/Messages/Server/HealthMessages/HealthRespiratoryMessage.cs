using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update respiratory health stats
/// </summary>
public class HealthRespiratoryMessage : ServerMessage
{
	public class HealthRespiratoryMessageNetMessage : ActualMessage
	{
		public bool IsSuffocating;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as HealthRespiratoryMessageNetMessage;
		if(newMsg == null) return;

		PlayerManager.LocalPlayerScript.playerHealth?.UpdateClientRespiratoryStats(newMsg.IsSuffocating);
	}

	public static HealthRespiratoryMessageNetMessage Send(GameObject entityToUpdate, bool IsSuffocating)
	{
		HealthRespiratoryMessageNetMessage msg = new HealthRespiratoryMessageNetMessage
		{
			IsSuffocating = IsSuffocating
		};
		new HealthRespiratoryMessage().SendTo(entityToUpdate, msg);
		return msg;
	}
}
