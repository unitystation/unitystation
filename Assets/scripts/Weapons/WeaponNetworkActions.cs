using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Weapons;

public class WeaponNetworkActions : NetworkBehaviour {

	[Command]
	public void CmdLoadMagazine(GameObject weapon, GameObject magazine){
		Weapon_Ballistic w = weapon.GetComponent<Weapon_Ballistic>();
		NetworkInstanceId nID = magazine.GetComponent<NetworkIdentity>().netId;
		w.magNetID = nID;
	}

	[Command]
	public void CmdUnloadWeapon(GameObject weapon){
		Weapon_Ballistic w = weapon.GetComponent<Weapon_Ballistic>();
		NetworkInstanceId newID = NetworkInstanceId.Invalid;
		w.magNetID = newID;
	}

	[Command]
	public void CmdShootBullet(Vector2 direction, string bulletName){
		GameObject bullet = GameObject.Instantiate(Resources.Load(bulletName) as GameObject,transform.position, Quaternion.identity);
		NetworkServer.Spawn(bullet);
		var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
		b.Shoot(direction, angle);

	}

	[Command]
	public void CmdAttackMob(GameObject npcObj)
	{
		Living attackTarget = npcObj.GetComponent<Living>();
		attackTarget.RpcReceiveDamage();
	}

	[Command]
	public void CmdHarvestMob(GameObject npcObj)
	{
		Living attackTarget = npcObj.GetComponent<Living>();
		attackTarget.RpcHarvest();
	}
}
