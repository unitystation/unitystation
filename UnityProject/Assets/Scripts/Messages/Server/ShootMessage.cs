using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Informs all clients that a shot has been performed so they can display it (but they needn't
/// perform any damage calculation, this is just displaying the shot that the server has already validated).
/// </summary>
public class ShootMessage : ServerMessage {

	public static short MessageType = (short)MessageTypes.ShootMessage;

	/// <summary>
	/// GameObject of the player performing the shot
	/// </summary>
	public NetworkInstanceId Shooter;
	/// <summary>
	/// Weapon being used to perform the shot
	/// </summary>
	public NetworkInstanceId Weapon;
	/// <summary>
	/// Where the shot ends (always originates from ShotBy)
	/// </summary>
	public Vector2 EndPos;
	/// <summary>
	/// targeted body part
	/// </summary>
	public BodyPartType DamageZone;
	/// <summary>
	/// If the shot is aimed at the shooter
	/// </summary>
	public bool IsSuicideShot;

	///To be run on client
	public override IEnumerator Process()
	{
		//		Logger.Log("Processed " + ToString());
		if (Shooter.Equals(NetworkInstanceId.Invalid)) {
			//Failfast
			Logger.LogWarning($"Shoot request invalid, processing stopped: {ToString()}", Category.Firearms);
			yield break;
		}

		yield return WaitFor(Shooter, Weapon);
		Weapon wep = NetworkObjects[1].GetComponent<Weapon>();
		//only needs to run on the clients other than the shooter
		if (!wep.isServer && PlayerManager.LocalPlayer.gameObject !=  NetworkObjects[0])
		{
			wep.DisplayShot(NetworkObjects[0], EndPos, DamageZone, IsSuicideShot);
		}
	}

	/*
	private void Shoot(Weapon weapon, GameObject shotByGO){
		if(shotByGO == PlayerManager.LocalPlayer || CustomNetworkManager.Instance._isServer){
			return;
		}

		if (weapon != null) {
			SoundManager.PlayAtPosition(weapon.FireingSound, shotByGO.transform.position);
		}

		GameObject bullet = PoolManager.Instance.PoolClientInstantiate(Resources.Load(BulletName) as GameObject,
		shotByGO.transform.position, Quaternion.identity);
		Vector2 dir = (EndPos - (Vector2)shotByGO.transform.position).normalized;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
		b.Shoot(dir, angle, shotByGO, DamageZone);
	}*/

	/// <summary>
	/// Tell all clients + server to perform a shot with the specified parameters.
	/// </summary>
	/// <param name="endPos">position shot should end (start position will be the shooter)</param>
	/// <param name="damageZone">body part being targeted</param>
	/// <param name="shooter">gameobject of player making the shot</param>
	/// <param name="isSuicide">if the shooter is shooting themselves</param>
	/// <returns></returns>
	public static ShootMessage SendToAll(Vector2 endPos, BodyPartType damageZone, GameObject shooter, GameObject weapon, bool isSuicide)
	{
		var msg = new ShootMessage {
			Weapon = weapon ? weapon.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			EndPos = endPos,
			DamageZone = damageZone,
			Shooter = shooter ? shooter.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			IsSuicideShot = isSuicide
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
		DamageZone = (BodyPartType)reader.ReadUInt32();
		Shooter = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Weapon);
		writer.Write(EndPos);
		writer.Write((int)DamageZone);
		writer.Write(Shooter);
	}
}
