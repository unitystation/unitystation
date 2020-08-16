using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;

public class GunRoulette : Gun
{
	[HideInInspector]
	public int untestedChambers = 6;
	public bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (CurrentMagazine == null)
		{
			PlayEmptySFX();
			if (interaction.Performer != PlayerManager.LocalPlayer)
			{
				Logger.LogTrace("Server rejected shot - No magazine being loaded", Category.Firearms);
			}
			return false;
		}
		else
		{
			Chat.AddActionMsgToChat(interaction.Performer, "You point the revolver at your head, pulling the trigger", $"{interaction.Performer.ExpensiveName()} points the revolver at their head and pulls the trigger!");
			int yes = Random.Range(1,untestedChambers);
			if (yes == 1)
			{
				untestedChambers = 6;
				Chat.AddActionMsgToChat(interaction.Performer, "You shoot yourself point blank in the head!", $"{interaction.Performer.ExpensiveName()} shoots themself point blank in the head!");
				return true;
			}
			else
			{
				untestedChambers--;
				PlayEmptySFX();
				Chat.AddActionMsgToChat(interaction.Performer, "The revolver makes a clicking sound", "The revolver makes a clicking sound");
				return false;
			}
		}
	}

	public bool Interact(HandActivate interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer, "You spin the cylinder of the revolver", $"{interaction.Performer.ExpensiveName()} spins the cylinder of the revolver ");
		SoundManager.PlayNetworkedAtPos("RevolverSpin", transform.position, sourceObj: serverHolder);
		WaitFor.Seconds(0.2f);
		untestedChambers = 6;
		return true;
	}
	private void PlayEmptySFX()
	{
		SoundManager.PlayNetworkedAtPos("EmptyGunClick", transform.position, sourceObj: serverHolder);
	}

	public void DisplayShot(GameObject shooter, Vector2 finalDirection, BodyPartType damageZone, bool isSuicideShot)
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
		GameObject bullet = Spawn.ClientPrefab(Projectile.name,
		shooter.transform.position, parent: shooter.transform.parent).GameObject;
		var b = bullet.GetComponent<Projectile>();
		b.Suicide(shooter, this, BodyPartType.Head);
		SoundManager.PlayAtPosition(FiringSound, shooter.transform.position, shooter);
		shooter.GetComponent<PlayerSprites>().ShowMuzzleFlash();
	}
}