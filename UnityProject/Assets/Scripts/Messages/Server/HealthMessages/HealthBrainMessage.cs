using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update brain health stats
/// </summary>
public class HealthBrainMessage : ServerMessage
{
	public struct HealthBrainMessageNetMessage : NetworkMessage
	{
		public uint EntityToUpdate;
		public bool IsHusk;
		public int BrainDamage;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public HealthBrainMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as HealthBrainMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
