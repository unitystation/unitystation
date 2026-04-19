using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Items.Traits;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Systems.Construction.Parts;
using US13.Systems.Inventory;
using US13.UI.Core.ProgressBar;
using Util;

namespace US13.Objects
{
	public class InternalBattery : MonoBehaviour, IChargeable, ICheckedInteractable<InventoryApply>, IServerSpawn
	{
		[SerializeField] private ItemStorage batteryStorage;
		[field: SerializeField] public bool isRemovableBattery { get; private set; } = true;
		[SerializeField, ShowIf(nameof(isRemovableBattery))] private float removeBatteryTime;

		[SerializeField] private bool ChangeSprites = false;
		[SerializeField, ShowIf(nameof(ChangeSprites))] private SpriteHandler EffectedHandler;
		public bool HasBatteries => batteryStorage.HasAnyOccupied();
		public bool IsFull => batteryStorage.GetNextEmptySlot() != null;

		private int currentCharge;

		public int CurrentCharge
		{
			get => currentCharge;
			set
			{
				if (currentCharge == value) return;

				int amountToLose = currentCharge - value;
				bool isLoss = amountToLose > 0;

				int amountToChange = isLoss ? amountToLose : -amountToLose;
				foreach (Battery battery in batteries)
				{
					int batteryCap = isLoss ? battery.Watts : battery.MaxWatts - battery.Watts;
					int chargeDifference = Math.Min(amountToChange, batteryCap);
					battery.Watts += isLoss ? -chargeDifference : chargeDifference;
					amountToChange -= chargeDifference;
					if (amountToChange <= 0) break;
				}

				currentCharge = Math.Max(0, value);
			}
		}

		private List<Battery> batteries;
		// Start is called before the first frame update

		private StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.ItemTransfer);

		public void OnSpawnServer(SpawnInfo info)
		{
			foreach (ItemSlot slot in batteryStorage.GetItemSlots())
			{
				if (slot.Item == null) continue;
				if (slot.Item.TryGetComponent<Battery>(out var battery) == false) continue;
				batteries.Add(battery);
			}
			EffectedHandler.SetSpriteVariant(batteries.Count);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot && isRemovableBattery)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver)) return true;
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell)
				    && interaction.UsedObject != null && IsFull == false) return true;
			}
			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (isRemovableBattery && Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && batteries.Count > 0) RemoveCellInteraction(interaction);
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell) == false ||
			    batteryStorage.GetNextEmptySlot() == null) return;

			if (interaction.UsedObject.TryGetComponent<Battery>(out Battery battery) == false) return;
			AddNewBattery(battery, interaction.FromSlot);
		}

		private void RemoveCellInteraction(InventoryApply interaction)
		{
			void ProgressFinishAction()
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"The {gameObject.ExpensiveName()}'s power cell pops out",
					$"{interaction.Performer.ExpensiveName()} finishes removing {gameObject.ExpensiveName()}'s energy cell.");
				RemoveBattery(interaction.FromSlot);
			}

			var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
				.ServerStartProgress(interaction.Performer.RegisterTile(), removeBatteryTime, interaction.Performer);

			if (bar != null)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You begin unsecuring the {gameObject.ExpensiveName()}'s power cell.",
					$"{interaction.Performer.ExpensiveName()} begins unsecuring {gameObject.ExpensiveName()}'s power cell.");
				AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver, interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: interaction.Performer);
			}

		}

		private void AddNewBattery(Battery batteryToAdd, ItemSlot fromSlot)
		{
			if (Inventory.ServerTransfer(fromSlot, batteryStorage.GetNextEmptySlot()))
			{
				batteries.Add(batteryToAdd);
				EffectedHandler.SetSpriteVariant(batteries.Count);
			}
		}

		public void RemoveBattery(ItemSlot toSlot = null)
		{
			ItemSlot slotToRemove = batteryStorage.GetTopOccupiedIndexedSlot();
			if (slotToRemove == null) return;
			Pickupable itemToRemove = slotToRemove.Item;

			if (toSlot == null || Inventory.ServerTransfer(batteryStorage.GetTopOccupiedIndexedSlot(), toSlot))
			{
				itemToRemove.TryGetComponent<Battery>(out var batteryToRemove);
				batteries.Remove(batteryToRemove);
				EffectedHandler.SetSpriteVariant(batteries.Count);
			}
		}


		public bool IsFullyCharged
		{
			get
			{
				foreach (var battery in batteries)
				{
					if (battery.IsFullyCharged == false) return false;
				}
				return true;
			}
		}

		public void ChargeBy(float watts)
		{
			CurrentCharge += (int)watts;
		}

		public float InternalResistanceParrallel()
		{
			float internalResistance = 0;
			foreach (Battery battery in batteries)
			{
				internalResistance += 1 / (float)battery.InternalResistance;
			}

			return 1 / internalResistance;
		}
	}
}
