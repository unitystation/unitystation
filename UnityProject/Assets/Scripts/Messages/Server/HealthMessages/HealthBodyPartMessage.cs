using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update body part health stats
/// </summary>
public class HealthBodyPartMessage : ServerMessage
{
	public uint EntityToUpdate;
	public BodyPartType BodyPart;
	public float BruteDamage;
	public float BurnDamage;

	public override void Process()
	{
		LoadNetworkObject(EntityToUpdate);
		if (NetworkObject != null){
			NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBodyPartStats(BodyPart, BruteDamage, BurnDamage);
		}
	}

	public static HealthBodyPartMessage Send(GameObject recipient, GameObject entityToUpdate, BodyPartType bodyPartType,
		float bruteDamage, float burnDamage)
	{
		HealthBodyPartMessage msg = new HealthBodyPartMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				BodyPart = bodyPartType,
				BruteDamage = bruteDamage,
				BurnDamage = burnDamage

		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthBodyPartMessage SendToAll(GameObject entityToUpdate, BodyPartType bodyPartType,
		float bruteDamage, float burnDamage)
	{
		HealthBodyPartMessage msg = new HealthBodyPartMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				BodyPart = bodyPartType,
				BruteDamage = bruteDamage,
				BurnDamage = burnDamage
		};
		msg.SendToAll();
		return msg;
	}
}