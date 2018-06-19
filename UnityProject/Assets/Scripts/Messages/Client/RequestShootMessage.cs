using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Weapons;

/// <summary>
/// Informs server of a shoot action
/// </summary>
public class RequestShootMessage : ClientMessage {

	public static short MessageType = (short)MessageTypes.RequestShootMessage;
	public NetworkInstanceId Weapon;
	public NetworkInstanceId ShotBy;
	public Vector2 Direction;
	public string BulletName;
	public BodyPartType DamageZone;
	public bool IsSuicideShot;


	public override IEnumerator Process()
	{
		if (ShotBy.Equals(NetworkInstanceId.Invalid) || Weapon.Equals(NetworkInstanceId.Invalid)) {
			//Failfast
			Debug.LogWarning($"Shoot request invalid, processing stopped: {ToString()}");
			yield break;
		}


		yield return WaitFor(SentBy, ShotBy, Weapon);
		Weapon wep = NetworkObjects[2].GetComponent<Weapon>();
		wep.ServerShoot(NetworkObjects[1],Direction,BulletName, DamageZone, IsSuicideShot);
	}

	public static RequestShootMessage Send(GameObject weapon, Vector2 direction, string bulletName,
	                                       BodyPartType damageZone, bool isSuicideShot, GameObject shotBy)
	{
		RequestShootMessage msg = new RequestShootMessage {
			Weapon = weapon ? weapon.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			Direction = direction,
			BulletName = bulletName,
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
		Weapon = reader.ReadNetworkId();
		Direction = reader.ReadVector2();
		BulletName = reader.ReadString();
		DamageZone = (BodyPartType)reader.ReadUInt32();
		IsSuicideShot = reader.ReadBoolean();
		ShotBy = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Weapon);
		writer.Write(Direction);
		writer.Write(BulletName);
		writer.Write((int)DamageZone);
		writer.Write(IsSuicideShot);
		writer.Write(ShotBy);
	}
}
