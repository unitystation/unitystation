using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update brain health stats
/// </summary>
public class HealthBrainMessage : ServerMessage
{
	public class HealthBrainMessageNetMessage : ActualMessage
	{
		public uint EntityToUpdate;
		public bool IsHusk;
		public int BrainDamage;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as HealthBrainMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.EntityToUpdate);
		if(NetworkObject != null) NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBrainStats(newMsg.IsHusk, newMsg.BrainDamage);
	}

	public static HealthBrainMessageNetMessage Send(GameObject recipient, GameObject entityToUpdate, bool isHusk, int brainDamage)
	{
		HealthBrainMessageNetMessage msg = new HealthBrainMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			IsHusk = isHusk,
			BrainDamage = brainDamage

		};
		new HealthBrainMessage().SendTo(recipient, msg);
		return msg;
	}

	public static HealthBrainMessageNetMessage SendToAll(GameObject entityToUpdate, bool isHusk, int brainDamage)
	{
		HealthBrainMessageNetMessage msg = new HealthBrainMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
			IsHusk = isHusk,
			BrainDamage = brainDamage
		};
		new HealthBrainMessage().SendToAll(msg);
		return msg;
	}
}
