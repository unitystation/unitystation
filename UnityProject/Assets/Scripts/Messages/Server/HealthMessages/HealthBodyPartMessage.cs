using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update body part health stats
/// </summary>
public class HealthBodyPartMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.HealthBodyPartStats;

	public NetworkInstanceId EntityToUpdate;
	public BodyPartType BodyPart;
	public float BruteDamage;
	public float BurnDamage;

	public override IEnumerator Process()
	{
		yield return WaitFor(EntityToUpdate);
		NetworkObject.GetComponent<LivingHealthBehaviour>().UpdateClientBodyPartStats(BodyPart, BruteDamage, BurnDamage);
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