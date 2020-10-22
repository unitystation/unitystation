using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;

public class GunElectrical : Gun
{
	public List<GameObject> firemodeProjectiles = new List<GameObject>();

	public List<string> firemodeFiringSound = new List<string>();

	public List<string> firemodeName = new List<string>();

	private int currentFiremode = 0;

	public int countFiremode = 1;

	public override void OnSpawnServer(SpawnInfo info)
	{
		base.OnSpawnServer(info);
		UpdateFiremode();
	}

	public void OnSpawnClient(SpawnInfo info)
	{
		UpdateFiremode();
	}

	public override bool Interact(HandActivate interaction)
	{
		if (countFiremode == 1) return false;
		if (countFiremode - 1 == currentFiremode )
		{
		currentFiremode = 0;
		}
		else
		{
			currentFiremode += 1;
		}
		UpdateFiremode();
		return true;
	}

	public void ServerInteract(HandActivate interaction)
	{

		if (countFiremode - 1 == currentFiremode )
		{
			currentFiremode = 0;
		}
		else
		{
			currentFiremode += 1;
		}
		UpdateFiremode();
	}

	public void UpdateFiremode()
	{
		if (countFiremode != 1)
		{
			CurrentMagazine.Projectile = firemodeProjectiles[currentFiremode];
			FiringSound = firemodeFiringSound[currentFiremode];
		}
	}

	public override String Examine(Vector3 pos)
	{
		string returnstring = WeaponType + " - Fires " + ammoType + " ammunition (" + (CurrentMagazine != null ? (CurrentMagazine.ServerAmmoRemains.ToString() + " rounds loaded in magazine") : "It's empty!") + ")";

		if (countFiremode != 1) {
			returnstring += "\nIt is set to " + firemodeName[currentFiremode] + " mode.";
		}

		return returnstring;
	}
}
