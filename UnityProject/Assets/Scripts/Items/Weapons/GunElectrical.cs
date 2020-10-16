using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;

public class GunElectrical : Gun
{
	public GameObject Projectile;

	public List<GameObject> firemodeProjectiles = new List<GameObject>();

	public List<string> firemodeFiringSound = new List<string>();

	public List<string> firemodeName = new List<string>();


	private int currentFiremode = 0;
	public int countFiremode = 1;

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

	public override void DisplayShot(GameObject shooter, Vector2 finalDirection,
		BodyPartType damageZone, bool isSuicideShot)
	{
		if (!MatrixManager.IsInitialized) return;

		//if this is our gun (or server), last check to ensure we really can shoot
		if ((isServer || PlayerManager.LocalPlayer == shooter) &&
			CurrentMagazine.ClientAmmoRemains <= 0)
		{
			if (isServer)
			{
				Logger.LogTrace("Server rejected shot - out of ammo", Category.Firearms);
			}

			return;
		}
		//TODO: If this is not our gun, simply display the shot, don't run any other logic
		if (shooter == PlayerManager.LocalPlayer)
		{
			//this is our gun so we need to update our predictions
			FireCountDown += 1.0 / FireRate;
			//add additional recoil after shooting for the next round
			AppendRecoil();

			//Default camera recoil params until each gun is configured separately
			if (CameraRecoilConfig == null || CameraRecoilConfig.Distance == 0f)
			{
				CameraRecoilConfig = new CameraRecoilConfig
				{
					Distance = 0.2f,
					RecoilDuration = 0.05f,
					RecoveryDuration = 0.6f
				};
			}
			Camera2DFollow.followControl.Recoil(-finalDirection, CameraRecoilConfig);
		}

		if (CurrentMagazine == null)
		{
			Logger.Log("Why is CurrentMagazine null on this client?");
		}
		else
		{
			//call ExpendAmmo outside of previous check, or it won't run serverside and state will desync.
			CurrentMagazine.ExpendAmmo();
		}

		//display the effects of the shot

		//get the bullet prefab being shot

		if (isSuicideShot)
		{
			GameObject bullet = Spawn.ClientPrefab(Projectile.name,
				shooter.transform.position, parent: shooter.transform.parent).GameObject;
			var b = bullet.GetComponent<Projectile>();
			b.Suicide(shooter, this, damageZone);
		}
		else
		{
			for (int n = 0; n < CurrentMagazine.ProjectilesFired; n++)
			{
				GameObject Abullet = Spawn.ClientPrefab(Projectile.name,
					shooter.transform.position, parent: shooter.transform.parent).GameObject;
				var A = Abullet.GetComponent<Projectile>();
				var finalDirectionOverride = CalcDirection(finalDirection, n);
				A.Shoot(finalDirectionOverride, shooter, this, damageZone);
			}
		}
		SoundManager.PlayAtPosition(FiringSound, shooter.transform.position, shooter);
		shooter.GetComponent<PlayerSprites>().ShowMuzzleFlash();
	}

	public void UpdateFiremode()
	{
		if (countFiremode != 1)
		{
		Projectile = firemodeProjectiles[currentFiremode];
		FiringSound = firemodeFiringSound[currentFiremode];
		}
	}

	public override String Examine(Vector3 pos)
	{
		if (countFiremode == 1) return "";
		return $"\nIt is set to {firemodeName[currentFiremode]} mode.";

	}
}
