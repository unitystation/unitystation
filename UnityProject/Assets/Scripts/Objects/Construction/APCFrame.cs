using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Logs;
using UnityEngine;
using Mirror;
using ScriptableObjects;
using Objects.Engineering;
using Systems.Electricity;

namespace Objects.Construction
{
	/// <summary>
	/// Main Component for APC Construction
	/// </summary>
	public class APCFrame : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState cablesAddedState = null;
		[SerializeField] private StatefulState powerControlAddedState = null;
		[SerializeField] private StatefulState powerCellAddedState = null;
		[SerializeField] private StatefulState wrenchedState = null;

		private UniversalObjectPhysics objectBehaviour;
		private Integrity integrity;
		private SpriteHandler spriteHandler;

		private ItemSlot powerCellSlot = null;
		private ItemSlot powerControlSlot = null;

		private Stateful stateful;

		[Tooltip("The Item trait for power cell machine parts")]
		[SerializeField] private ItemTrait powerCellTrait = null;
		[Tooltip("APC gameObject for this frame to create")]
		[SerializeField] private GameObject APCObject = null;
		private StatefulState CurrentState => stateful.CurrentState;

		private void Awake()
		{
			powerControlSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			powerCellSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(1);
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();

			if (!CustomNetworkManager.IsServer) return;

			integrity = GetComponent<Integrity>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();

			if (CurrentState != powerCellAddedState)
			{
				stateful.ServerChangeState(initialState);
			}
		}
		private void OnEnable()
		{
			try
			{
				integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
			}
			catch (NullReferenceException exception)
			{
				Loggy.LogError($"Catched a NRE in APCFrame OnEnable() {exception.Message} \n {exception.StackTrace}", Category.Electrical);
			}
		}
		private void OnDisable()
		{
			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}

		/// <summary>
		/// Client Side interaction
		/// </summary>
		/// <param name="interaction"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			//different logic depending on state
			if (CurrentState == initialState)
			{
				//Adds the power control module or deconstruct
				return (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) && Validations.HasUsedAtLeast(interaction, 5)) ||
					Validations.HasUsedActiveWelder(interaction);
			}
			else if (CurrentState == cablesAddedState)
			{
				//cut cables or add power control module
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter) ||
					  Validations.HasItemTrait(interaction, CommonTraits.Instance.PowerControlBoard);
			}
			else if (CurrentState == powerControlAddedState)
			{
				//Remove power control module or add power cell
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) ||
					Validations.HasItemTrait(interaction, powerCellTrait);
			}
			else if (CurrentState == powerCellAddedState)
			{
				//wrench on cover or crowbar out power control module which removes the power cell too
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) ||
					   Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}
			else if (CurrentState == wrenchedState)
			{
				//screw in parts or crowbar off the cover
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
					   Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}

			return false;
		}

		/// <summary>
		/// What the server does if the interaction is valid on client
		/// </summary>
		/// <param name="interaction"></param>
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (CurrentState == initialState)
			{
				InitialStateInteraction(interaction);
			}
			else if (CurrentState == cablesAddedState)
			{
				CablesAddedStateInteraction(interaction);
			}
			else if (CurrentState == powerControlAddedState)
			{
				PowerControlAddedStateInteraction(interaction);
			}
			else if (CurrentState == powerCellAddedState)
			{
				PowerCellAddedStateInteraction(interaction);
			}
			else if (CurrentState == wrenchedState)
			{
				WrenchedStateInteraction(interaction);
			}
		}

		/// <summary>
		/// Stage 1, Add cable to continue or welder to destroy frame.
		/// </summary>
		/// <param name="interaction"></param>
		private void InitialStateInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) &&
									 Validations.HasUsedAtLeast(interaction, 5))
			{
				//add 5 cables
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start adding cables to the frame...",
					$"{interaction.Performer.ExpensiveName()} starts adding cables to the frame...",
					"You add cables to the frame.",
					$"{interaction.Performer.ExpensiveName()} adds cables to the frame.",
					() =>
					{
						Inventory.ServerConsume(interaction.HandSlot, 5);
						stateful.ServerChangeState(cablesAddedState);
					});
			}
			else if (Validations.HasUsedActiveWelder(interaction))
			{
				//deconsruct
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start deconstructing the frame...",
					$"{interaction.Performer.ExpensiveName()} starts deconstructing the frame...",
					"You deconstruct the frame.",
					$"{interaction.Performer.ExpensiveName()} deconstructs the frame.",
					() =>
					{
						Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 2);
						_ = Despawn.ServerSingle(gameObject);
					});
			}
		}

		/// <summary>
		/// Stage 2, Add the power control module to continue contruction, or wirecutters to move to stage 1
		/// </summary>
		/// <param name="interaction"></param>
		private void CablesAddedStateInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter))
			{
				//cut out cables
				Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
					$"{interaction.Performer.ExpensiveName()} removes the cables.");
				ToolUtils.ServerPlayToolSound(interaction);
				Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 5);
				stateful.ServerChangeState(initialState);

				spriteHandler.ChangeSprite((int)SpriteStates.Frame);
			}
		    else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.PowerControlBoard))
			{
				//stick in the circuit board
				Chat.AddActionMsgToChat(interaction, $"You place the {interaction.UsedObject.ExpensiveName()} inside the frame.",
					$"{interaction.Performer.ExpensiveName()} places the {interaction.UsedObject.ExpensiveName()} inside the frame.");
				Inventory.ServerTransfer(interaction.HandSlot, powerControlSlot);

				stateful.ServerChangeState(powerControlAddedState);

				spriteHandler.ChangeSprite((int)SpriteStates.FrameCircuit);
			}
		}

		/// <summary>
		/// Stage 3, Add in power cell, or remove power control module to go back to stage 2.
		/// </summary>
		/// <param name="interaction"></param>
		private void PowerControlAddedStateInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
			{
				Chat.AddActionMsgToChat(interaction, $"You remove the power control module from the frame.",
					$"{interaction.Performer.ExpensiveName()} removes the power control module from the frame.");
				ToolUtils.ServerPlayToolSound(interaction);

				RemoveCircuitAndParts();
			}
			else if (Validations.HasItemTrait(interaction, powerCellTrait))
			{
				var usedObject = interaction.UsedObject;

				Chat.AddActionMsgToChat(interaction, $"You place the {usedObject.ExpensiveName()} inside the frame.",
					$"{interaction.Performer.ExpensiveName()} places the {usedObject.ExpensiveName()} inside the frame.");
				Inventory.ServerTransfer(interaction.HandSlot, powerCellSlot);

				stateful.ServerChangeState(powerCellAddedState);

				spriteHandler.ChangeSprite((int)SpriteStates.FramePower);
			}
		}

		/// <summary>
		/// Stage 4, secure the cover, or remove board and return to stage 2.
		/// </summary>
		/// <param name="interaction"></param>
		private void PowerCellAddedStateInteraction(HandApply interaction)
		{
			//Complete construction, spawn new machine and send data over to it.
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				//secure the APC's cover
				ToolUtils.ServerPlayToolSound(interaction);
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You begin securing the cover to the frame...",
					$"{interaction.Performer.ExpensiveName()} begins securing the cover to the frame...",
					"You secure the cover to the frame.",
					$"{interaction.Performer.ExpensiveName()} secures the cover to the frame.",
					() =>
					{
						stateful.ServerChangeState(wrenchedState);
						spriteHandler.ChangeSprite((int)SpriteStates.FrameWrenched);
					});

			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
			{
				//Remove the the circuit board and power cell.
				Chat.AddActionMsgToChat(interaction, $"You remove the power control module from the frame.",
					$"{interaction.Performer.ExpensiveName()} removes the power control module from the frame.");
				ToolUtils.ServerPlayToolSound(interaction);

				RemoveCircuitAndParts();
			}
		}

		/// <summary>
		/// Stage 5, complete construction, or remove cover and return to stage 4.
		/// </summary>
		/// <param name="interaction"></param>
		private void WrenchedStateInteraction(HandApply interaction)
		{
			//Complete construction, spawn new machine and send data over to it.
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				Chat.AddActionMsgToChat(interaction, $"You secure the electronics to the APC.",
					$"{interaction.Performer.ExpensiveName()} secures the electronics to the APC.");
				ToolUtils.ServerPlayToolSound(interaction);

				MatrixInfo matrix = MatrixManager.AtPoint(gameObject.AssumedWorldPosServer(), true);

				var localPosInt = MatrixManager.WorldToLocalInt(gameObject.AssumedWorldPosServer(), matrix);

				var econs = interaction.Performer.GetComponentInParent<Matrix>().GetElectricalConnections(localPosInt);
				foreach (var Connection in econs.List)
				{
					if (Connection.Categorytype == PowerTypeCategory.APC)
					{
						econs.Pool();
						return;
					}
				}

				econs.Pool();

				GameObject WallMount = Spawn.ServerPrefab(APCObject, gameObject.AssumedWorldPosServer(), interaction.Performer.transform.parent, spawnItems: false).GameObject;

				var Directional = WallMount.GetComponent<Rotatable>();
				if (Directional != null) Directional.FaceDirection(gameObject.GetComponent<Rotatable>().CurrentDirection);

				ItemSlot apcPowerControlSlot = WallMount.GetComponent<ItemStorage>().GetIndexedItemSlot(0);
				ItemSlot apcPowerCellSlot = WallMount.GetComponent<ItemStorage>().GetIndexedItemSlot(1);

				Inventory.ServerTransfer(powerControlSlot, apcPowerControlSlot);
				Inventory.ServerTransfer(powerCellSlot, apcPowerCellSlot);

				//Despawn frame
				_ = Despawn.ServerSingle(gameObject);
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
			{
				//Remove the cover
				ToolUtils.ServerPlayToolSound(interaction);
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You begin prying the cover from the frame...",
					$"{interaction.Performer.ExpensiveName()} begins prying the cover from the frame...",
					"You pry the cover from the frame.",
					$"{interaction.Performer.ExpensiveName()} pries the cover from the frame.",
					() =>
					{
						stateful.ServerChangeState(powerCellAddedState);
						spriteHandler.ChangeSprite((int)SpriteStates.FramePower);
					});

			}
		}

		/// <summary>
		/// Examine messages
		/// </summary>
		/// <param name="worldPos"></param>
		/// <returns></returns>
		public string Examine(Vector3 worldPos)
		{
			string msg = "";
			if (CurrentState == initialState)
			{
				msg = " Add five wires to continue construction. Or use a welder to deconstruct.\n";
			}
			if (CurrentState == cablesAddedState)
			{
				msg = " Add a power control module to the frame to continue construction. Or use a wirecutter to remove the cables.\n";
			}
			if (CurrentState == powerControlAddedState)
			{
				msg = "Add a power cell module to continue contruction. Or use a crowbar to remove power control module.\n";
			}
			if (CurrentState == powerCellAddedState)
			{
				msg = "Use a wrench the secure the cover or use crowbar to remove power control module.\n";
			}
			if (CurrentState == wrenchedState)
			{
				msg = "Use a screwdriver to finish construction or use crowbar to remove the cover.\n";
			}

			return msg;
		}

		/// <summary>
		/// Initializes this frame's state to be from a just-deconstructed machine
		/// </summary>
		/// <param name="machine"></param>
		public void ServerInitFromComputer(APC apc)
		{
			ItemSlot apcPowerControlSlot = apc.GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			ItemSlot apcPowerCellSlot = apc.GetComponent<ItemStorage>().GetIndexedItemSlot(1);

			Inventory.ServerTransfer(apcPowerControlSlot, powerControlSlot);
			Inventory.ServerTransfer(apcPowerCellSlot, powerCellSlot);

			spriteHandler.ChangeSprite((int)SpriteStates.FrameWrenched);

			// Set initial state
			objectBehaviour.SetIsNotPushable(true);
			stateful.ServerChangeState(wrenchedState);
		}

		public void WhenDestroyed(DestructionInfo info)
		{
			if (CurrentState == powerCellAddedState || CurrentState == powerControlAddedState)
			{
				RemoveCircuitAndParts();
			}
			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}

		private void RemoveCircuitAndParts()
		{
			Inventory.ServerDrop(powerControlSlot);
			Inventory.ServerDrop(powerCellSlot);

			stateful.ServerChangeState(cablesAddedState);
			spriteHandler.ChangeSprite((int)SpriteStates.Frame);
		}

		private enum SpriteStates
		{
			Frame = 0,
			FrameCircuit = 1,
			FramePower = 2,
			FrameWrenched = 3,
		}
	}
}
