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
	public bool IsSuffocating;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientRespiratoryStats(IsSuffocating);
	}

	public static HealthRespiratoryMessage Send(GameObject recipient, GameObject entityToUpdate, bool IsSuffocating)
	{
		HealthRespiratoryMessage msg = new HealthRespiratoryMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsSuffocating = IsSuffocating
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthRespiratoryMessage SendToAll(GameObject entityToUpdate, bool IsSuffocating)
	{
		HealthRespiratoryMessage msg = new HealthRespiratoryMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				IsSuffocating = IsSuffocating
		};
		msg.SendToAll();
		return msg;
	}
}
