using Objects.Atmospherics;
using System.Collections.Generic;
using UnityEngine;

namespace Items.Others
{
	/// <summary>
	/// A jetpack that allows players to move freely in low gravity tiles (such as in space)
	/// It does this by force pushing the player to the direction they're facing when a movement key is pressed.
	/// </summary>
	public class Jetpack : MonoBehaviour, IInteractable<InventoryApply>, ICheckedInteractable<HandActivate>, IServerInventoryMove
	{
		[SerializeField] private float gasReleaseOnUse = 0.2f;
		private float moveQueueBuildUp = 0.15f;
		private bool isOn;
		private bool compatibleSlot = false;
		private OrientationEnum lastRotation = OrientationEnum.Default;
		private GasContainer gasContainer;
		private PlayerScript player;

		private const string PARTICLE_ID = "JetpackTrail";

		public readonly HashSet<NamedSlot> CompatibleSlots = new HashSet<NamedSlot>() {
			NamedSlot.leftHand,
			NamedSlot.rightHand,
			NamedSlot.suitStorage,
			NamedSlot.belt,
			NamedSlot.back,
			NamedSlot.suitStorage,
		};


		private void Awake()
		{
			gasContainer = GetComponent<GasContainer>();
		}

		private void OnDisable()
		{
			if (isOn) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PushUpdate);
		}


		public void OnInventoryMoveServer(InventoryMove info)
		{
			//was it transferred from a player's visible inventory?
			if (info.FromPlayer != null)
			{
				compatibleSlot = false;
				OffState();
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
			if (player.PlayerSync.IsPressedServer) //Is a movement key pressed?
			{
				PushPlayerInFacedDirection(player, gasContainer, gasReleaseOnUse, moveQueueBuildUp);
				moveQueueBuildUp += 0.1f;
				moveQueueBuildUp = Mathf.Clamp(moveQueueBuildUp, 0.1f, 2f);
			}
			else
			{
				moveQueueBuildUp = 0.25f;
			}
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			ToggleState(interaction);
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			ToggleState(interaction);
		}

		private void ToggleState(Interaction interaction)
		{
			if (interaction == null)
			{
				OffState();
				return;
			}
			isOn = !isOn;
			Chat.AddExamineMsg(interaction.Performer, isOn ? "You open the valve" : "You close the valve");
			if (isOn)
			{
				player = interaction.PerformerPlayerScript;
				player.PlayerDirectional.OnRotationChange.AddListener(OnPlayerRotationChange);
				UpdateManager.Add(PushUpdate, 0.25f);
				player.Particles.ToggleParticle(PARTICLE_ID, true);
			}
			else
			{
				OffState();
			}
		}

		private void OffState()
		{
			isOn = false;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PushUpdate);
			if (player == null) return;
			player.PlayerDirectional.OnRotationChange.RemoveListener(OnPlayerRotationChange);
			player.Particles.ToggleParticle(PARTICLE_ID, false);
			player = null;
		}

		private void OnPlayerRotationChange(OrientationEnum rot)
		{
			if (isOn == false || lastRotation == rot) return;
			PushPlayerInFacedDirection(player, gasContainer, gasReleaseOnUse, 1.5f * moveQueueBuildUp);
		}

		public static void PushPlayerInFacedDirection(PlayerScript playerScript, GasContainer gasContainer, float gasRelease = 5, float speed = 1f)
		{
			if (playerScript.ObjectPhysics.CanPush(playerScript.CurrentDirection.ToLocalVector2Int()) == false) return;
			playerScript.ObjectPhysics.NewtonianPush(playerScript.CurrentDirection.ToLocalVector2Int(), speed);
			var domGas = gasContainer.GasMix.GetBiggestGasSOInMix();
			if (domGas != null) gasContainer.GasMix.RemoveGas(domGas, gasRelease);
		}
	}
}
