using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using UnityEditor;
using Weapons;
using Mirror;
using Weapons.Projectiles;
using SoundMessages;

public class GunElectrical : Gun, ICheckedInteractable<HandActivate>
{
	public List<GameObject> firemodeProjectiles = new List<GameObject>();
	public List<AddressableAudioSource> firemodeFiringSound = new List<AddressableAudioSource>();
	public List<string> firemodeName = new List<string>();
	public List<int> firemodeUsage = new List<int>();

	private const float magRemoveTime = 3f;

	[SerializeField]
	private bool allowScrewdriver = true;

	[SyncVar(hook = nameof(UpdateFiremode))]
	private int currentFiremode = 0;

	public Battery battery =>
			magSlot.Item != null ? magSlot.Item.GetComponent<Battery>() : null;

	public ElectricalMagazine currentElectricalMag =>
			magSlot.Item != null ? magSlot.Item.GetComponent<ElectricalMagazine>() : null;

	public override void OnSpawnServer(SpawnInfo info)
	{
		UpdateFiremode(currentFiremode, 0);
		base.OnSpawnServer(info);
	}

	public override bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public override bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (side == NetworkSide.Server && DefaultWillInteract.Default(interaction, side)) return true;

		//only reload if the gun is the target and mag/clip is in hand slot
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot && side == NetworkSide.Client)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && allowScrewdriver ||
				Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) ||
				Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.FiringPin))
			{
				return true;
			}
			else if (interaction.UsedObject != null)
			{
				MagazineBehaviour mag = interaction.UsedObject.GetComponent<MagazineBehaviour>();
				if (mag && Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (battery == null || firemodeUsage[currentFiremode] > battery.Watts)
		{
			PlayEmptySFX();
			return false;
		}
		CurrentMagazine.containedBullets[0] = firemodeProjectiles[currentFiremode];
		currentElectricalMag.toRemove = firemodeUsage[currentFiremode];
		return base.WillInteract(interaction, side);
	}

	public override void ServerPerformInteraction(HandActivate interaction)
	{
		if (firemodeProjectiles.Count <= 1)
		{
			return;
		}
		if (currentFiremode == firemodeProjectiles.Count - 1)
		{
			UpdateFiremode(currentFiremode, 0);
		}
		else
		{
			UpdateFiremode(currentFiremode, currentFiremode + 1);
		}
		Chat.AddExamineMsgFromServer(interaction.Performer, $"You switch your {gameObject.ExpensiveName()} into {firemodeName[currentFiremode]} mode");
		CurrentMagazine.ServerSetAmmoRemains(battery.Watts / firemodeUsage[currentFiremode]);
	}

	public override void ServerPerformInteraction(AimApply interaction)
	{
		if (firemodeUsage[currentFiremode] > battery.Watts) return;
		base.ServerPerformInteraction(interaction);
		CurrentMagazine.ServerSetAmmoRemains(battery.Watts / firemodeUsage[currentFiremode]);
	}

	public override void ServerPerformInteraction(InventoryApply interaction)
	{
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && CurrentMagazine != null && allowScrewdriver)
			{
				PowerCellRemoval(interaction);
			}
			MagazineBehaviour mag = interaction.UsedObject.GetComponent<MagazineBehaviour>();
			if (mag)
			{
				base.RequestReload(mag.gameObject);
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) && allowPinSwap)
			{
				PinRemoval(interaction);
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.FiringPin) && allowPinSwap)
			{
				PinAddition(interaction);
			}
		}
	}

	private void PowerCellRemoval(InventoryApply interaction)
	{
		void ProgressFinishAction()
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"The {gameObject.ExpensiveName()}'s power cell pops out",
				$"{interaction.Performer.ExpensiveName()} finishes removing {gameObject.ExpensiveName()}'s energy cell.");
			base.RequestUnload(CurrentMagazine);				
		}

		var bar = StandardProgressAction.Create(base.ProgressConfig, ProgressFinishAction)
			.ServerStartProgress(interaction.Performer.RegisterTile(), magRemoveTime, interaction.Performer);

		if (bar != null)
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You begin unsecuring the {gameObject.ExpensiveName()}'s power cell.",
				$"{interaction.Performer.ExpensiveName()} begins unsecuring {gameObject.ExpensiveName()}'s power cell.");
				AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.screwdriver, interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: serverHolder);
		}
	}

	public void UpdateFiremode(int oldValue, int newState)
	{
		currentFiremode = newState;
		FiringSoundA = firemodeFiringSound[currentFiremode];
		//TODO: change sprite here
	}

	public override String Examine(Vector3 pos)
	{
		string returnstring = WeaponType + " - Fires " + ammoType + " ammunition (" +
		(CurrentMagazine != null ? (Mathf.Floor(battery.Watts / firemodeUsage[currentFiremode]) + " rounds loaded in magazine") : "It's empty!") + ")\n" +
		(FiringPin != null ? "It has a " + FiringPin.gameObject.ExpensiveName() + " installed." : "It doesn't have a firing pin installed, and won't fire.");

		if (firemodeProjectiles.Count > 1) {
			returnstring += "\nIt is set to " + firemodeName[currentFiremode] + " mode.";
		}

		return returnstring;
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(GunElectrical), true)]
	public class GunEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
		}
	}
#endif
}
