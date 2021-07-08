using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Mirror;
using UnityEngine;
using Systems.Electricity;

namespace Chemistry
{
	/// <summary>
	/// Main component for chemistry dispenser.
	/// </summary>
	public class ChemistryDispenser : NetworkBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable
	{
		public ReagentContainer Container => itemSlot != null && itemSlot.ItemObject != null
			? itemSlot.ItemObject.GetComponent<ReagentContainer>()
			: null;

		public delegate void ChangeEvent();

		public static event ChangeEvent changeEvent;

		private ItemStorage itemStorage;
		private ItemSlot itemSlot;

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
			itemSlot = itemStorage.GetIndexedItemSlot(0);
		}

		private void UpdateGUI()
		{
			// Change event runs updateAll in GUI_ChemistryDispenser
			if (changeEvent != null)
			{
				changeEvent();
			}
		}


		private ItemSlot GetBestSlot(GameObject item, ConnectedPlayer subject)
		{
			if (subject == null)
			{
				return default;
			}

			var playerStorage = subject.Script.DynamicItemStorage;
			return playerStorage.GetBestHandOrSlotFor(item);
		}

		public void EjectContainer(ConnectedPlayer player)
		{
			if (!Inventory.ServerTransfer(itemSlot, GetBestSlot(itemSlot.ItemObject, player)))
			{
				Inventory.ServerDrop(itemSlot);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

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

		public PowerState ThisState;

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			ThisState = state;
		}

		#endregion
	}
}
