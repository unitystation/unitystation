using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interactions.Internal;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Transform;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Objects;
using US13.Player;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Others
{
	/// <summary>
	/// A jetpack that allows players to move freely in low gravity tiles (such as in space)
	/// It does this by force pushing the player to the direction they're facing when a movement key is pressed.
	/// </summary>
	public class Jetpack : NetworkBehaviour, IInteractable<InventoryApply>, ICheckedInteractable<HandActivate>, IServerInventoryMove
	{
		[SerializeField] private float gasReleaseOnUse = 0.2f;
		private float moveQueueBuildUp = 0.1f;

		[SyncVar(hook = nameof(SyncisOn))] public bool isOn;
		[SyncVar] private bool compatibleSlot = false;


		[SyncVar] private bool HasGas = false;

		private OrientationEnum lastRotation = OrientationEnum.Default;
		private GasContainer gasContainer;
		private PlayerScript player => gameObject.GetRootGameObject().GetComponentCustom<PlayerScript>();

		private const string PARTICLE_ID = "JetpackTrail";
		private const float MINIMUM_FLIGHT_BUILDUP_SPEED = 0.1f;
		private const float MAX_FLIGHT_BUILDUP_SPEED = 2f;

		public readonly HashSet<NamedSlot> CompatibleSlots = new HashSet<NamedSlot>() {
			NamedSlot.leftHand,
			NamedSlot.rightHand,
			NamedSlot.suitStorage,
			NamedSlot.belt,
			NamedSlot.back,
			NamedSlot.suitStorage,
		};

		private int Frame;

		private void Awake()
		{
			gasContainer = GetComponent<GasContainer>();
		}

		private void OnDisable()
		{
			if (isOn)
			{
				SyncisOn(isOn, false);
			}
		}


		public void OnInventoryMoveServer(InventoryMove info)
		{
			//was it transferred from a player's visible inventory?
			if (info.FromPlayer != null)
			{
				compatibleSlot = false;
				SyncisOn(isOn, false);
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

			if (CustomNetworkManager.IsServer)
			{

				if (gasContainer.GasMixLocal.Moles <= 0)
				{
					HasGas = false;
					return;
				}
				else
				{
					HasGas = true;
				}
			}
			else
			{
				if (HasGas == false)
				{
					return;
				}
			}


			if (CustomNetworkManager.IsServer ?  player.PlayerSync.IsPressedServer: player.PlayerSync.IsPressedCashed)//Is a movement key pressed?
			{
				PushPlayerInFacedDirection(player, gasContainer, gasReleaseOnUse, moveQueueBuildUp);
				moveQueueBuildUp += 0.1f;
				moveQueueBuildUp = Mathf.Clamp(moveQueueBuildUp, MINIMUM_FLIGHT_BUILDUP_SPEED, MAX_FLIGHT_BUILDUP_SPEED);
			}
			else
			{
				moveQueueBuildUp -= 0.25f;
				moveQueueBuildUp = Mathf.Clamp(moveQueueBuildUp, MINIMUM_FLIGHT_BUILDUP_SPEED, MAX_FLIGHT_BUILDUP_SPEED);
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
				SyncisOn(isOn, false);
				OffState();
				return;
			}
			SyncisOn(isOn, !isOn);
			Chat.AddExamineMsg(interaction.Performer, isOn ? "You open the valve" : "You close the valve");


		}

		private void OffState()
		{
			moveQueueBuildUp = 0.1f;
			Chat.AddLocalMsgToChat($"The safety mechanism on the valve makes a pop as it securely shuts off the {gameObject.ExpensiveName()}.", gameObject, doSpeechBubble : false);
			if (player == null) return;
			player.PlayerDirectional.OnRotationChange.RemoveListener(OnPlayerRotationChange);
			if (CustomNetworkManager.IsServer)
			{
				player.Particles.ServerToggleParticle(PARTICLE_ID, false);
			}

		}

		private void SyncisOn(bool oldisOn, bool newisOn)
		{
			isOn = newisOn;
			if (newisOn)
			{
				UpdateManager.Add(PushUpdate, 0.25f);
				player.PlayerDirectional.OnRotationChange.AddListener(OnPlayerRotationChange);
				if (CustomNetworkManager.IsServer)
				{
					player.Particles.ServerToggleParticle(PARTICLE_ID, true);
				}

			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PushUpdate);
				OffState();
			}

		}

		private void OnPlayerRotationChange(OrientationEnum rot)
		{
			if (isOn == false || lastRotation == rot) return;
			if (Frame == Time.frameCount) return;
			Frame = Time.frameCount;
			PushPlayerInFacedDirection(player, gasContainer, gasReleaseOnUse, 2f);
 		}

		public static void PushPlayerInFacedDirection(PlayerScript playerScript, GasContainer gasContainer, float gasRelease = 5, float speed = 1f)
		{
			if (playerScript.ObjectPhysics.CanPush(playerScript.CurrentDirection.ToLocalVector2Int()) == false) return;
			playerScript.ObjectPhysics.NewtonianPush(playerScript.CurrentDirection.ToLocalVector2Int(), speed);
			if (CustomNetworkManager.IsServer)
			{
				var domGas = gasContainer.GasMixLocal.GetBiggestGasSOInMix();
				if (domGas != null) gasContainer.GasMixLocal.RemoveGas(domGas, gasRelease);
			}
		}
	}
}
