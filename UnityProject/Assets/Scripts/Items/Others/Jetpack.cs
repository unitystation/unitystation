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
	public class Jetpack : MonoBehaviour, IInteractable<InventoryApply>, IServerInventoryMove
	{
		[SerializeField] private float gasReleaseOnUse = 0.2f;
		private Pickupable pickupable;
		private GasContainer gasContainer;

		private bool isOn;
		private bool compatibleSlot = false;

		private PlayerScript player;

		public HashSet<NamedSlot> CompatibleSlots = new HashSet<NamedSlot>() {
			NamedSlot.leftHand,
			NamedSlot.rightHand,
			NamedSlot.suitStorage,
			NamedSlot.belt,
			NamedSlot.back,
			NamedSlot.suitStorage,
		};


		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			gasContainer = GetComponent<GasContainer>();
		}

		private void OnDisable()
		{
			if(isOn) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PushUpdate);
		}


		public void OnInventoryMoveServer(InventoryMove info)
		{
			//was it transferred from a player's visible inventory?
			if (info.FromPlayer != null)
			{
				compatibleSlot = false;
			}

			if (info.ToPlayer != null)
			{
				if (CompatibleSlots.Contains(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)))
				{
					compatibleSlot = true;
				}
			}
		}


		private void PushUpdate()
		{
			if (isOn == false || compatibleSlot == false) return;
			if (gasContainer.GasMix.Moles <= 0) return;
			if (player.PlayerSync.IsPressedServer)//Is movement pressed?
			{
				PushPlayerInFacedDirection(player, gasContainer, gasReleaseOnUse);
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

		public static void PushPlayerInFacedDirection(PlayerScript playerScript, GasContainer gasContainer, float gasRelease = 10)
		{
			if (playerScript.objectPhysics.CanPush(playerScript.CurrentDirection.ToLocalVector2Int()) == false) return;
			playerScript.objectPhysics.NewtonianPush(playerScript.CurrentDirection.ToLocalVector2Int(),1f);
			var domGas = gasContainer.GasMix.GetBiggestGasSOInMix();
			if (domGas != null) gasContainer.GasMix.RemoveGas(domGas, gasRelease);
		}
	}
}
