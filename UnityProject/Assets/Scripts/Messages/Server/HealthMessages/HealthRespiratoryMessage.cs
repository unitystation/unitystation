using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update respiratory health stats
/// </summary>
public class HealthRespiratoryMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthRespiratoryStats;

	public NetworkInstanceId EntityToUpdate;
	public bool IsBreathing;
	public bool IsSuffocating;

	public int PressureStatus;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientRespiratoryStats(IsBreathing, IsSuffocating, PressureStatus);
	}

	public static HealthRespiratoryMessage Send(GameObject recipient, GameObject entityToUpdate, bool isBreathing, bool IsSuffocating, int pressureStatus)
	{
		HealthRespiratoryMessage msg = new HealthRespiratoryMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsBreathing = isBreathing,
				IsSuffocating = IsSuffocating,
				PressureStatus = pressureStatus,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthRespiratoryMessage SendToAll(GameObject entityToUpdate,  bool isBreathing, bool IsSuffocating, int pressureStatus)
	{
		HealthRespiratoryMessage msg = new HealthRespiratoryMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsBreathing = isBreathing,
				IsSuffocating = IsSuffocating,
				PressureStatus = pressureStatus
		};
		msg.SendToAll();
		return msg;
	}
}
