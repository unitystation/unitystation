using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using Mirror;
using Weapons.Projectiles;

public class GunElectrical : Gun, ICheckedInteractable<HandActivate>
{
	public List<GameObject> firemodeProjectiles = new List<GameObject>();
	public List<string> firemodeFiringSound = new List<string>();
	public List<string> firemodeName = new List<string>();
	public List<int> firemodeUsage = new List<int>();

	[SyncVar(hook = nameof(UpdateFiremode))]
	private int currentFiremode = 0;

	public int CurrentFiremode => currentFiremode;

	public Battery battery =>
			magSlot.Item != null ? magSlot.Item.GetComponent<Battery>() : null;

	public ElectricalMagazine currentelmag =>
			magSlot.Item != null ? magSlot.Item.GetComponent<ElectricalMagazine>() : null;

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public override bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (firemodeUsage[currentFiremode] > battery.Watts) 
		{
			base.PlayEmptySFX();
			return false;
		}
		CurrentMagazine.containedBullets[0] = firemodeProjectiles[currentFiremode];
		currentelmag.toRemove = firemodeUsage[currentFiremode];
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

	public override void ServerPerformInteraction(AimApply interaction)
	{
		if (firemodeUsage[currentFiremode] > battery.Watts) return;
		base.ServerPerformInteraction(interaction);		
	}

	public void UpdateFiremode(int oldValue, int newState)
	{
		currentFiremode = newState;
		FiringSound = firemodeFiringSound[currentFiremode];
		//TODO: change sprite here
	}

	public override String Examine(Vector3 pos)
	{
		string returnstring = WeaponType + " - Fires " + ammoType + " ammunition (" + (CurrentMagazine != null ? (Mathf.Floor(battery.Watts / firemodeUsage[currentFiremode]) + " rounds loaded in magazine") : "It's empty!") + ")";

		if (firemodeProjectiles.Count > 1) {
			returnstring += "\nIt is set to " + firemodeName[currentFiremode] + " mode.";
		}

		return returnstring;
	}
}
