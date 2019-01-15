using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Informs server of a shoot action
/// </summary>
public class RequestShootMessage : ClientMessage {

	public static short MessageType = (short)MessageTypes.RequestShootMessage;
	/// <summary>
	/// NetId of the player attempting the shot
	/// </summary>
	public NetworkInstanceId ShotBy;
	/// <summary>
	/// Position the player is shooting at (NOT the same as the final position - final position
	/// may differ due to accuracy penalty)
	/// </summary>
	public Vector2 Target;
	/// <summary>
	/// Targeted damage zone
	/// </summary>
	public BodyPartType DamageZone;
	/// <summary>
	/// Whether this is a suicide shot
	/// </summary>
	public bool IsSuicideShot;


	public override IEnumerator Process()
	{
		if (ShotBy.Equals(NetworkInstanceId.Invalid)) {
			//Failfast
			Logger.LogWarning($"Shoot request invalid, processing stopped: {ToString()}", Category.Firearms);
			yield break;
		}


		//TODO: Shouldn't need SentBy
		yield return WaitFor(SentBy, ShotBy);
		//get the currently equipped weapon in the player's active hand
		PlayerNetworkActions pna = NetworkObjects[1].GetComponent<PlayerNetworkActions>();
		Weapon wep = pna.GetActiveHandItem().GetComponent<Weapon>();
		wep.ServerShoot(NetworkObjects[1],Target, DamageZone, IsSuicideShot);
	}

	public static RequestShootMessage Send(GameObject weapon, Vector2 direction, string bulletName,
	                                       BodyPartType damageZone, bool isSuicideShot, GameObject shotBy)
	{
		RequestShootMessage msg = new RequestShootMessage {
			Target = direction,
			DamageZone = damageZone,
			IsSuicideShot = isSuicideShot,
			ShotBy = shotBy ? shotBy.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid
		};
		msg.Send();
		return msg;
	}

	public override string ToString()
	{
		return "";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Target = reader.ReadVector2();
		DamageZone = (BodyPartType)reader.ReadUInt32();
		IsSuicideShot = reader.ReadBoolean();
		ShotBy = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Target);
		writer.Write((int)DamageZone);
		writer.Write(IsSuicideShot);
		writer.Write(ShotBy);
	}
}
