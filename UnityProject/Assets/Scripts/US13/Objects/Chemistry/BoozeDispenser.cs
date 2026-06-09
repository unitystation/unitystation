using Mirror;
using SecureStuff;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers;
using US13.Objects.Engineering;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Inventory;
using Util;

namespace US13.Objects.Chemistry
{
	/// <summary>
	/// Main component for chemistry dispenser.
	/// </summary>
	public class BoozeDispenser : NetworkBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable
	{
		public ReagentContainer Container => itemSlot != null && itemSlot.ItemObject != null
			? itemSlot.ItemObject.GetCachedComponent<ReagentContainer>(includeDisabled: false)
			: null;

		public delegate void ChangeEvent();

		public static event ChangeEvent changeEvent;

		private ItemSlot itemSlot;

		public ItemStorage itemStorage;

		private void Awake()
		{
			itemSlot = itemStorage.GetIndexedItemSlot(0);
		}

		private void UpdateGUI()
		{
			// Change event runs updateAll in ChemistryGUI
			if (changeEvent != null)
			{
				changeEvent();
			}
		}

		private ItemSlot GetBestSlot(GameObject item, PlayerInfo subject)
		{
			if (subject == null)
			{
				return default;
			}

			var playerStorage = subject.Script.DynamicItemStorage;

			//Can be null if Ai is using machine
			if (playerStorage == null)
			{
				return null;
			}

			return playerStorage.GetBestHandOrSlotFor(item);
		}

		/// <summary>
		/// Ejects input container from ChemMaster into best slot available and clears the buffer
		/// </summary>
		/// <param name="subject"></param>
		public void EjectContainer(PlayerInfo subject)
		{
			var bestSlot = GetBestSlot(itemSlot.ItemObject, subject);

			if (bestSlot == null)
			{
				Inventory.ServerDrop(itemSlot);
				return;
			}

			if (!Inventory.ServerTransfer(itemSlot, bestSlot))
			{
				Inventory.ServerDrop(itemSlot);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only interaction that works is using a reagent container on this
			if (!Validations.HasComponent<ReagentContainer>(interaction.HandObject)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//put the reagant container inside me
			Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
			UpdateGUI();
		}

		#region IAPCPowerable

		[PlayModeOnly] public PowerState ThisState;

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			ThisState = state;
		}

		#endregion
	}
}
