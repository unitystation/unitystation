using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using Mirror;


public class GunElectrical : Gun, ICheckedInteractable<HandActivate>
{
	public List<GameObject> firemodeProjectiles = new List<GameObject>();
	public List<string> firemodeFiringSound = new List<string>();
	public List<string> firemodeName = new List<string>();

	[SyncVar(hook = nameof(UpdateFiremode))]
	private int currentFiremode = 0;

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public override bool WillInteract(AimApply interaction, NetworkSide side)
	{
		CurrentMagazine.containedBullets[0] = firemodeProjectiles[currentFiremode];
		return base.WillInteract(interaction, side);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (firemodeProjectiles.Count <= 1)
			return;
		if (currentFiremode == firemodeProjectiles.Count - 1)
			currentFiremode = 0;
		else
		{
			currentFiremode++;
		}
		Chat.AddExamineMsgToClient($"You switch your {gameObject.ExpensiveName()} into {firemodeName[currentFiremode]} mode");
	}

	public void UpdateFiremode(int oldValue, int newState)
	{
		currentFiremode = newState;
		FiringSound = firemodeFiringSound[currentFiremode];
		//TODO: change sprite here
	}

	public override String Examine(Vector3 pos)
	{
		string returnstring = WeaponType + " - Fires " + ammoType + " ammunition (" + (CurrentMagazine != null ? (CurrentMagazine.ServerAmmoRemains.ToString() + " rounds loaded in magazine") : "It's empty!") + ")";

		if (firemodeProjectiles.Count > 1) {
			returnstring += "\nIt is set to " + firemodeName[currentFiremode] + " mode.";
		}

		return returnstring;
	}
}
