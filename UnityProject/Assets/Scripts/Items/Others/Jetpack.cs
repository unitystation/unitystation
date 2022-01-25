using Objects.Atmospherics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items.Others
{
	/// <summary>
	/// A jetpack that allows players to move freely in low gravity tiles (such as in space)
	/// It does this by force pushing the player to the direction they're facing when a movement key is pressed.
	/// </summary>
	public class Jetpack : MonoBehaviour, IInteractable<InventoryApply>
	{
		[SerializeField] private float gasReleaseOnUse = 0.2f;
		private Pickupable pickupable;
		private GasContainer gasContainer;

		private bool isOn;
		private PlayerScript player;


		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			gasContainer = GetComponent<GasContainer>();
		}

		private void OnDisable()
		{
			if(isOn) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PushUpdate);
		}

		/// <summary>
		/// The jetpack is only usable inside the player's inventory slots so we check if it's
		/// in his hand or equipment slot.
		/// </summary>
		private bool CheckForInventory()
		{
			//Is the jetpack inside a slot right now?
			if (pickupable.ItemSlot == null) return false;
			//Is this slot a player one? True if hand or equipment false if in a backpack or such.
			if (pickupable.ItemSlot.Player == null) return false;
			return true;
		}

		private void PushUpdate()
		{
			if (isOn == false || CheckForInventory() == false) return;
			if (gasContainer.GasMix.Moles <= 0) return;
			if (player.PlayerSync.InputMovementDetected)//Is movement pressed?
			{
				if (player.pushPull.TryPush(player.CurrentDirection.ToLocalVector2Int(), player.playerMove.RunSpeed, true))
				{
					var domGas = gasContainer.GasMix.GetBiggestGasSOInMix();
					if (domGas != null) gasContainer.GasMix.RemoveGas(domGas, gasReleaseOnUse);
				}
			}
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			isOn = !isOn;
			Chat.AddExamineMsg(interaction.Performer, isOn ? "You open the valve" : "You close the valve");
			if (isOn)
			{
				player = interaction.PerformerPlayerScript;
				UpdateManager.Add(PushUpdate, 0.1f);
				return;
			}
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PushUpdate);
		}
	}
}
