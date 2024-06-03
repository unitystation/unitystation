using System;
using System.Collections.Generic;
using System.Text;
using AddressableReferences;
using Messages.Server.SoundMessages;
using Mirror;
using Systems.Construction.Parts;
using UnityEditor;
using UnityEngine;
using Weapons.Projectiles;

namespace Weapons
{
	public class GunElectrical : Gun, ICheckedInteractable<HandActivate>
	{
		public List<GameObject> firemodeProjectiles = new List<GameObject>();
		public List<AddressableAudioSource> firemodeFiringSound = new List<AddressableAudioSource>();
		public List<string> firemodeName = new List<string>();
		public List<int> firemodeUsage = new List<int>();

		[SerializeField, Tooltip("(Optional) Charge Sprite, must have variants equal to amount of charge bars")]
		public SpriteHandler chargeSprite;

		[SerializeField, Tooltip("(Optional) Ammo Sprite")]
		public SpriteHandler ammoSprite;

		private const int NO_CELL_SPRITE = 2;
		private const int FULL_SPRITE = 3;

		private const float magRemoveTime = 3f;

		[SerializeField]
		private bool allowScrewdriver = true;

		[SyncVar(hook = nameof(UpdateFiremode))]
		private int currentFiremode = 0;

		public Battery Battery =>
				magSlot.Item != null ? magSlot.Item.GetComponent<Battery>() : null;

		public ElectricalMagazine CurrentElectricalMag =>
			magSlot.Item != null ? magSlot.Item.GetComponent<ElectricalMagazine>() : null;

		public override void OnSpawnServer(SpawnInfo info)
		{
			UpdateFiremode(currentFiremode, 0);
			base.OnSpawnServer(info);
			UpdateChargeSprite();
		}

		public override void OnInventoryMoveServer(InventoryMove info)
		{
			base.OnInventoryMoveServer(info);
			//update sprite when moved into or out of inventory to handle
			//weapon chargers and other things modifying charge whilst
			//the weapon is out of a players inventory
			UpdateChargeSprite();
		}


		public override bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
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
			CurrentMagazine.ServerSetAmmoRemains(Battery.Watts / firemodeUsage[currentFiremode]);
			UpdateChargeSprite();
		}

		public override bool WillInteract(AimApply interaction, NetworkSide side)
		{
			if (Battery == null || firemodeUsage[currentFiremode] > Battery.Watts)
			{
				PlayEmptySfx();
				return false;
			}
			CurrentMagazine.containedBullets[0] = firemodeProjectiles[currentFiremode];
			CurrentElectricalMag.toRemove = firemodeUsage[currentFiremode];
			return base.WillInteract(interaction, side);
		}

		public override void ServerPerformInteraction(AimApply interaction)
		{
			if (firemodeUsage[currentFiremode] > Battery.Watts) return;
			base.ServerPerformInteraction(interaction);
			CurrentMagazine.ServerSetAmmoRemains(Battery.Watts / firemodeUsage[currentFiremode]);
			UpdateChargeSprite();
		}

		public override bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only reload if the gun is the target and mag/clip is in hand slot
			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
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

		public override void ServerPerformInteraction(InventoryApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && CurrentMagazine != null && allowScrewdriver)
			{
				PowerCellRemoval(interaction);
			}
			MagazineBehaviour mag = interaction.UsedObject.GetComponent<MagazineBehaviour>();
			if (mag)
			{
				ServerHandleReloadRequest(mag.gameObject);
				UpdateChargeSprite();
			}
			else
			{
				base.PinInteraction(interaction);
			}
		}

		private void PowerCellRemoval(InventoryApply interaction)
		{
			void ProgressFinishAction()
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"The {gameObject.ExpensiveName()}'s power cell pops out",
					$"{interaction.Performer.ExpensiveName()} finishes removing {gameObject.ExpensiveName()}'s energy cell.");
				base.ServerHandleUnloadRequest();
				UpdateChargeSprite();
			}

			var bar = StandardProgressAction.Create(base.ProgressConfig, ProgressFinishAction)
				.ServerStartProgress(interaction.Performer.RegisterTile(), magRemoveTime, interaction.Performer);

			if (bar != null)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You begin unsecuring the {gameObject.ExpensiveName()}'s power cell.",
					$"{interaction.Performer.ExpensiveName()} begins unsecuring {gameObject.ExpensiveName()}'s power cell.");
					AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver, interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: serverHolder);
			}
		}

		public void UpdateFiremode(int oldValue, int newState)
		{
			currentFiremode = newState;
			FiringSoundA = firemodeFiringSound[currentFiremode];
		}

		//This function should only be run serverside or it WILL desync the weapons sprites
		public void UpdateChargeSprite() {
			if (ammoSprite != null )
			{
				ammoSprite.SetCatalogueIndexSprite(currentFiremode + 2);
			}
			if (chargeSprite == null) {
				return;
			}
			if (Battery == null)
			{
				chargeSprite.SetCatalogueIndexSprite(NO_CELL_SPRITE);
			} else if (Battery.Watts / firemodeUsage[currentFiremode] != 0) {
				chargeSprite.SetCatalogueIndexSprite(FULL_SPRITE + currentFiremode);
				float rounds = Battery.Watts / firemodeUsage[currentFiremode];
				float maxrounds = Battery.MaxWatts / firemodeUsage[currentFiremode];
				int percent = Mathf.FloorToInt((rounds / maxrounds) * 100.0f);
				int outof = Mathf.CeilToInt(percent * chargeSprite.PresentSpritesSet.Variance.Count / 100.0f);
				chargeSprite.SetSpriteVariant(outof-1);
			} else {
				chargeSprite.PushClear();
			}
		}

		public override String Examine(Vector3 pos)
		{
			StringBuilder exam = new StringBuilder();
			exam.AppendLine($"{WeaponType} - Fires {ammoType} ammunition")
				.AppendLine(CurrentMagazine != null ? $"{Mathf.Floor(Battery.Watts / firemodeUsage[currentFiremode])} rounds loaded" : "It's empty!")
				.AppendLine(FiringPin != null ? $"It has a {FiringPin.gameObject.ExpensiveName()} installed" : "It doesn't have a firing pin installed, it won't fire")
				.Append(firemodeProjectiles.Count > 1 ? $"It is set to {firemodeName[currentFiremode]} mode." : "");
			return exam.ToString();
		}
	}
}
