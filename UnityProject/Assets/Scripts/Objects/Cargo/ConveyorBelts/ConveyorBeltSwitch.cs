using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ScriptableObjects;

namespace Construction.Conveyors
{
	/// <summary>
	/// Used for controlling conveyor belts.
	/// </summary>
	public class ConveyorBeltSwitch : NetworkBehaviour, ICheckedInteractable<HandApply>, ISetMultitoolSlaveMultiMaster
	{
		[Tooltip("Assign the conveyor belts this switch should control.")]
		[SerializeField]
		private List<ConveyorBelt> conveyorBelts = new List<ConveyorBelt>();

		[Tooltip("Conveyor belt speed.")]
		[SerializeField]
		private float ConveyorBeltSpeed = 0.5f;

		private SpriteHandler spriteHandler;

		public SwitchState CurrentState { get; private set; }

		private SwitchState prevMoveState;

		#region Lifecycle

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void OnDisable()
		{
			if (!isServer) return;

			SetState(SwitchState.Off);
		}

		public override void OnStartServer()
		{
			SetBeltInfo();
		}

		#endregion Lifecycle

		private void UpdateMe()
		{
			MoveConveyorBelts();
		}

		private void MoveConveyorBelts()
		{
			for (int i = 0; i < conveyorBelts.Count; i++)
			{
				if (conveyorBelts[i] != null) conveyorBelts[i].MoveBelt();
			}
		}

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			return interaction.HandObject == null ||
					Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				//deconsruct
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start deconstructing the conveyor belt switch...",
					$"{interaction.Performer.ExpensiveName()} starts deconstructing the conveyor belt switch...",
					"You deconstruct the conveyor belt switch.",
					$"{interaction.Performer.ExpensiveName()} deconstructs the conveyor belt switch.",
					DeconstructSwitch);
			}
			else
			{
				ToggleSwitch();
			}
		}

		private void ToggleSwitch()
		{
			switch (CurrentState)
			{
				case SwitchState.Off:
					if (prevMoveState == SwitchState.Forward)
					{
						SetState(SwitchState.Backward);
					}
					else if (prevMoveState == SwitchState.Backward)
					{
						SetState(SwitchState.Forward);
					}
					else
					{
						SetState(SwitchState.Forward);
					}
					prevMoveState = CurrentState;
					break;
				case SwitchState.Forward:
				case SwitchState.Backward:
					SetState(SwitchState.Off);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void DeconstructSwitch()
		{
			SetState(SwitchState.Off);
			conveyorBelts.Clear();
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 5);
			Despawn.ServerSingle(gameObject);
		}

		#endregion Interaction

		/// <summary>
		/// Allow these conveyor belts to be controlled by this switch.
		/// </summary>
		/// <param name="newConveyorBelts"> Conveyor belts to control </param>
		public void AddConveyorBelt(List<ConveyorBelt> newConveyorBelts)
		{
			foreach (var conveyor in newConveyorBelts)
			{
				if (!conveyorBelts.Contains(conveyor))
				{
					conveyorBelts.Add(conveyor);
				}
			}

			SetBeltInfo();
		}

		/// <summary>
		/// Hands a reference of this switch to the belts so any neighbour ones
		/// can automatically sync up
		/// </summary>
		private void SetBeltInfo()
		{
			for (int i = 0; i < conveyorBelts.Count; i++)
			{
				if (conveyorBelts[i] != null) conveyorBelts[i].SetSwitchRef(this);
			}
		}

		private void SetState(SwitchState newState)
		{
			CurrentState = newState;
			spriteHandler.ChangeSprite((int)CurrentState);

			if (CurrentState != SwitchState.Off)
			{
				UpdateManager.Add(UpdateMe, ConveyorBeltSpeed);
			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			}

			UpdateConveyorStates();
		}

		private void UpdateConveyorStates()
		{
			foreach (ConveyorBelt conveyor in conveyorBelts)
			{
				if (conveyor != null)
				{
					conveyor.UpdateState();
				}
			}
		}

		public enum SwitchState
		{
			Off = 0,
			Forward = 1,
			Backward = 2
		}

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.Conveyor;
		public MultitoolConnectionType ConType => conType;

		public void SetMasters(List<ISetMultitoolMaster> Imasters)
		{
			List<ConveyorBelt> InnewConveyorBelts = new List<ConveyorBelt>();
			foreach (var Conveyor in Imasters)
			{
				InnewConveyorBelts.Add(Conveyor as ConveyorBelt);
			}
			AddConveyorBelt(InnewConveyorBelts);
		}

		#endregion Multitool Interaction
	}
}
