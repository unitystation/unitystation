using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Weapons;
using PlayGroup;

public class ShootMessage : ServerMessage {

	public static short MessageType = (short)MessageTypes.ShootMessage;

	public NetworkInstanceId Weapon;
	public NetworkInstanceId ShotBy;
	public Vector2 EndPos;
	public string BulletName;
	public BodyPartType DamageZone;

	///To be run on client
	public override IEnumerator Process()
	{
		//		Debug.Log("Processed " + ToString());
		if (ShotBy.Equals(NetworkInstanceId.Invalid) || Weapon.Equals(NetworkInstanceId.Invalid)) {
			//Failfast
			Debug.LogWarning($"Shoot request invalid, processing stopped: {ToString()}");
			yield break;
		}

		yield return WaitFor(ShotBy, Weapon);
		Shoot(NetworkObjects[1], NetworkObjects[0]);
	}

	private void Shoot(GameObject weaponGO, GameObject shotByGO){
		if(shotByGO == PlayerManager.LocalPlayer || CustomNetworkManager.Instance._isServer){
			return;
		}

		Weapon wep = weaponGO.GetComponent<Weapon>();
		if (wep != null) {
			SoundManager.PlayAtPosition(wep.FireingSound, shotByGO.transform.position);
		}

		GameObject bullet = PoolManager.Instance.PoolClientInstantiate(Resources.Load(BulletName) as GameObject,
		shotByGO.transform.position, Quaternion.identity);
		Vector2 dir = (EndPos - (Vector2)shotByGO.transform.position).normalized;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
		b.Shoot(dir, angle, shotByGO, DamageZone);
	}

	//public static ShootMessage Send(GameObject weapon, Vector2 endPos, string bulletName,
	//									BodyPartType damageZone, GameObject shotBy)
	//{
	//	var msg = new ShootMessage {

	//	};
	//	msg.SendTo(recipient);
	//	return msg;
	//}

	/// <param name="transformedObject">object to hide</param>
	/// <param name="state"></param>
	/// <param name="forced">
	///     Used for client simulation, use false if already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	public static ShootMessage SendToAll(GameObject weapon, Vector2 endPos, string bulletName,
										   BodyPartType damageZone, GameObject shotBy)
	{
		var msg = new ShootMessage {
			Weapon = weapon ? weapon.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			EndPos = endPos,
			BulletName = bulletName,
			DamageZone = damageZone,
			ShotBy = shotBy ? shotBy.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid
		};
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return " ";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Weapon = reader.ReadNetworkId();
		EndPos = reader.ReadVector2();
		BulletName = reader.ReadString();
		DamageZone = (BodyPartType)reader.ReadUInt32();
		ShotBy = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Weapon);
		writer.Write(EndPos);
		writer.Write(BulletName);
		writer.Write((int)DamageZone);
		writer.Write(ShotBy);
	}
}
