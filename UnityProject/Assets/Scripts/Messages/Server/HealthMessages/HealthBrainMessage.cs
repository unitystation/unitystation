using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update brain health stats
/// </summary>
public class HealthBrainMessage : ServerMessage
{
	public uint EntityToUpdate;
	public bool IsHusk;
	public int BrainDamage;

	public override void Process()
	{
		LoadNetworkObject(EntityToUpdate);
		if(NetworkObject != null) NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBrainStats(IsHusk, BrainDamage);
	}

	public static HealthBrainMessage Send(GameObject recipient, GameObject entityToUpdate, bool isHusk, int brainDamage)
	{
		HealthBrainMessage msg = new HealthBrainMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			IsHusk = isHusk,
			BrainDamage = brainDamage

		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthBrainMessage SendToAll(GameObject entityToUpdate, bool isHusk, int brainDamage)
	{
		HealthBrainMessage msg = new HealthBrainMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			IsHusk = isHusk,
			BrainDamage = brainDamage
		};
		msg.SendToAll();
		return msg;
	}
}
