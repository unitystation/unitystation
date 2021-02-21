using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update body part health stats
/// </summary>
public class HealthBodyPartMessage : ServerMessage
{
	public class HealthBodyPartMessageNetMessage : NetworkMessage
	{
		public uint EntityToUpdate;
		public BodyPartType BodyPart;
		public float BruteDamage;
		public float BurnDamage;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as HealthBodyPartMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.EntityToUpdate);
		if (NetworkObject != null){
			NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBodyPartStats(newMsg.BodyPart, newMsg.BruteDamage, newMsg.BurnDamage);
		}
	}

	public static HealthBodyPartMessageNetMessage Send(GameObject recipient, GameObject entityToUpdate, BodyPartType bodyPartType,
		float bruteDamage, float burnDamage)
	{
		HealthBodyPartMessageNetMessage msg = new HealthBodyPartMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				BodyPart = bodyPartType,
				BruteDamage = bruteDamage,
				BurnDamage = burnDamage

		};
		new HealthBodyPartMessage().SendTo(recipient, msg);
		return msg;
	}

	public static HealthBodyPartMessageNetMessage SendToAll(GameObject entityToUpdate, BodyPartType bodyPartType,
		float bruteDamage, float burnDamage)
	{
		HealthBodyPartMessageNetMessage msg = new HealthBodyPartMessageNetMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				BodyPart = bodyPartType,
				BruteDamage = bruteDamage,
				BurnDamage = burnDamage
		};
		new HealthBodyPartMessage().SendToAll(msg);
		return msg;
	}
}