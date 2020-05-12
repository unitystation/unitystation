using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Informs all clients that a shot has been performed so they can display it (but they needn't
/// perform any damage calculation, this is just displaying the shot that the server has already validated).
/// </summary>
public class ShootMessage : ServerMessage {

	/// <summary>
	/// GameObject of the player performing the shot
	/// </summary>
	public uint Shooter;
	/// <summary>
	/// Weapon being used to perform the shot
	/// </summary>
	public uint Weapon;
	/// <summary>
	/// Direction of shot, originating from Shooter)
	/// </summary>
	public Vector2 Direction;
	/// <summary>
	/// targeted body part
	/// </summary>
	public BodyPartType DamageZone;
	/// <summary>
	/// If the shot is aimed at the shooter
	/// </summary>
	public bool IsSuicideShot;

	///To be run on client
	public override void Process()
	{
		if (!MatrixManager.IsInitialized) return;

		if (Shooter.Equals(NetId.Invalid)) {
			//Failfast
			Logger.LogWarning($"Shoot request invalid, processing stopped: {ToString()}", Category.Firearms);
			return;
		}

		//Not even spawned don't show bullets
		if (PlayerManager.LocalPlayer == null) return;

		LoadMultipleObjects(new uint[] {Shooter, Weapon});

		Gun wep = NetworkObjects[1].GetComponent<Gun>();
		if (wep == null)
		{
			return;
		}
		//only needs to run on the clients other than the shooter
		if (!wep.isServer && PlayerManager.LocalPlayer.gameObject !=  NetworkObjects[0])
		{
			wep.DisplayShot(NetworkObjects[0], Direction, DamageZone, IsSuicideShot);
		}
	}

	/// <summary>
	/// Tell all clients + server to perform a shot with the specified parameters.
	/// </summary>
	/// <param name="direction">Direction of shot from shooter</param>
	/// <param name="damageZone">body part being targeted</param>
	/// <param name="shooter">gameobject of player making the shot</param>
	/// <param name="isSuicide">if the shooter is shooting themselves</param>
	/// <returns></returns>
	public static ShootMessage SendToAll(Vector2 direction, BodyPartType damageZone, GameObject shooter, GameObject weapon, bool isSuicide)
	{
		var msg = new ShootMessage {
			Weapon = weapon ? weapon.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			Direction = direction,
			DamageZone = damageZone,
			Shooter = shooter ? shooter.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
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
		Weapon = reader.ReadUInt32();
		Direction = reader.ReadVector2();
		DamageZone = (BodyPartType)reader.ReadUInt32();
		Shooter = reader.ReadUInt32();
		IsSuicideShot = reader.ReadBoolean();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Weapon);
		writer.WriteVector2(Direction);
		writer.WriteInt32((int)DamageZone);
		writer.WriteUInt32(Shooter);
		writer.WriteBoolean(IsSuicideShot);
	}
}
