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
}
