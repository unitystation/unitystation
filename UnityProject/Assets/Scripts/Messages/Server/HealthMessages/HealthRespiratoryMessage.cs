using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update respiratory health stats
/// </summary>
public class HealthRespiratoryMessage : ServerMessage
{
	public struct HealthRespiratoryMessageNetMessage : NetworkMessage
	{
		public bool IsSuffocating;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public HealthRespiratoryMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as HealthRespiratoryMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
