using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
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
		[SerializeField, ShowIf(nameof(ChangeSprites))] private bool UseVariants = false;
		public bool HasBatteries => batteries.Any();
		public bool IsFull => batteryStorage.GetNextEmptySlot() == null;

		private int currentCharge = 0;

		public int CurrentCharge
		{
			get => currentCharge;
			set
			{
				if (currentCharge == value) return;

				int amountToLose = currentCharge - value;
				bool isLoss = amountToLose > 0;

				int amountToChange = isLoss ? amountToLose : -amountToLose;
				foreach (Battery battery in batteries.Values)
				{
					int batteryCap = isLoss ? battery.Watts : battery.MaxWatts - battery.Watts;
					int chargeDifference = Math.Min(amountToChange, batteryCap);
					battery.Watts += isLoss ? -chargeDifference : chargeDifference;
					amountToChange -= chargeDifference;
					if (amountToChange <= 0) break;
				}

				currentCharge = Math.Max(0, value);
				OnChargeChanged?.Invoke();
			}
		}


		public float MaxCharge {
			get
			{
				float maxwatts = 0;
				foreach (Battery battery in batteries.Values)
				{
					maxwatts += battery.MaxWatts;
				}
				return maxwatts;
			}

		}

		public Action OnChargeChanged;
		private Dictionary<ItemSlot,Battery> batteries;
		// Start is called before the first frame update

		private StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.ItemTransfer);

		public void OnSpawnServer(SpawnInfo info)
		{
			batteries = new Dictionary<ItemSlot,Battery>();
			currentCharge = 0;
			foreach (var slot in batteryStorage.GetItemSlots())
			{
				slot.OnSlotContentsChangeServer.AddListener(() =>  BatteriesChange(slot));
			}
			OnChargeChanged?.Invoke();
			UpdateSprites();
		}

		public void BatteriesChange(ItemSlot slot)
		{
			if (slot.Item == null || slot.Item.TryGetComponent<Battery>(out var battery) == false)
			{
				if (batteries.ContainsKey(slot))
				{
					currentCharge = Math.Max(currentCharge,0) - batteries[slot].Watts;
					batteries.Remove(slot);
				}
			}
			else
			{
				batteries[slot] = battery;
				currentCharge = Math.Max(currentCharge,0) + battery.Watts;
			}

			OnChargeChanged?.Invoke();
			UpdateSprites();
		}

		private void UpdateSprites()
		{
			if (ChangeSprites == false) return;
			if(UseVariants) EffectedHandler.SetSpriteVariant(batteries.Count);
			else EffectedHandler.SetCatalogueIndexSprite(batteries.Count);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;


			if (interaction.TargetObject.Equals(gameObject) && isRemovableBattery)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver)) return true;
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell) && IsFull == false) return true;
			}
			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (isRemovableBattery && interaction.UsedObject != null && Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && HasBatteries) RemoveCellInteraction(interaction);
			if ( Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell) == false || IsFull) return;
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
			if (fromSlot == null || Inventory.ServerTransfer(fromSlot, batteryStorage.GetNextEmptySlot()))
			{

			}
		}

		public void RemoveBattery(ItemSlot toSlot = null)
		{
			if (HasBatteries == false) return;
			ItemSlot slotToRemove = batteryStorage.GetTopOccupiedIndexedSlot();
			if (slotToRemove == null) return;
			Pickupable itemToRemove = slotToRemove.Item;

			if(toSlot == null || Inventory.ServerTransfer(slotToRemove, toSlot) == false) Inventory.ServerDrop(slotToRemove);
		}


		public bool IsFullyCharged
		{
			get
			{
				foreach (var battery in batteries)
				{
					if (battery.Value.IsFullyCharged == false) return false;
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
			foreach (Battery battery in batteries.Values)
			{
				internalResistance += 1 / (float)battery.InternalResistance;
			}

			return 1 / internalResistance;
		}
	}
}
