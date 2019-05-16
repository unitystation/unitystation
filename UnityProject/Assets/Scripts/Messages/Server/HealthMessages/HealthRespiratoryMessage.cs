using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update respiratory health stats
/// </summary>
public class HealthRespiratoryMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthRespiratoryStats;

	public uint EntityToUpdate;
	public bool IsBreathing;
	public bool IsSuffocating;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientRespiratoryStats(IsBreathing, IsSuffocating);
	}

	public static HealthRespiratoryMessage Send(GameObject recipient, GameObject entityToUpdate, bool isBreathing, bool IsSuffocating)
	{
		HealthRespiratoryMessage msg = new HealthRespiratoryMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsBreathing = isBreathing,
				IsSuffocating = IsSuffocating
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthRespiratoryMessage SendToAll(GameObject entityToUpdate,  bool isBreathing, bool IsSuffocating)
	{
		HealthRespiratoryMessage msg = new HealthRespiratoryMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsBreathing = isBreathing,
				IsSuffocating = IsSuffocating
		};
		msg.SendToAll();
		return msg;
	}
}
