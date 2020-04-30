using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Construction;

namespace Machines
{
	/// <summary>
	/// Main Component for Machine Construction
	/// </summary>
	public class MachineFrame : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState cablesAddedState = null;
		[SerializeField] private StatefulState wrenchedState = null;
		[SerializeField] private StatefulState circuitAddedState = null;
		[SerializeField] private StatefulState partsAddedState = null;
		[SerializeField] private StatefulState screwedFinishedState = null;

		private ItemSlot circuitBoardSlot;//Index 0
		private IDictionary<ItemTrait, int> partsUsed = new Dictionary<ItemTrait, int>();
		private List<GameObject> partsUsedGameObjects = new List<GameObject>();
		private Stateful stateful;

		private MachineParts machineParts;
		private MachineParts.MachinePartList trait;

		private StatefulState CurrentState => stateful.CurrentState;
		private ObjectBehaviour objectBehaviour;

		private void Awake()
		{
			circuitBoardSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			//different logic depending on state
			if (CurrentState == initialState)
			{
				//Add 5 cables or deconstruct
				return (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) && Validations.HasUsedAtLeast(interaction, 5)) ||
					Validations.HasUsedActiveWelder(interaction);
			}
			else if (CurrentState == cablesAddedState)
			{
				//cut cables or wrench frame
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter) ||
					  Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench);
			}
			else if (CurrentState == wrenchedState)
			{
				//Unwrench or add circuit board
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench) ||
					Validations.HasUsedComponent<ComputerCircuitboard>(interaction);
			}
			else if (CurrentState == circuitAddedState)
			{
				//remove circuit board
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					return true;
				}

				//check part item traits, if in scriptableObject of the machine then return true.
				foreach(var part in machineParts.machineParts)
				{
					if (Validations.HasUsedItemTrait(interaction, part.itemTrait))
					{
						return true;
					}
				}

				return false;
			}
			else if (CurrentState == partsAddedState)
			{
				//screw in parts or remove parts
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
					   Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}
			else if (CurrentState == screwedFinishedState)
			{
				//Unscrew parts
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver);
			}

			return false;

		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (CurrentState == initialState)
			{
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) &&
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
							Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 5);
							Despawn.ServerSingle(gameObject);
						});
				}
			}
			else if (CurrentState == cablesAddedState)
			{
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter))
				{
					//cut out cables
					Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
						$"{interaction.Performer.ExpensiveName()} removes the cables.");
					ToolUtils.ServerPlayToolSound(interaction);
					Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 5);
					stateful.ServerChangeState(initialState);
				}
				else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					if (!ServerValidations.IsAnchorBlocked(interaction))
					{
						//wrench in place
						ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
							"You start wrenching the frame into place...",
							$"{interaction.Performer.ExpensiveName()} starts wrenching the frame into place...",
							"You wrench the frame into place.",
							$"{interaction.Performer.ExpensiveName()} wrenches the frame into place.",
							() => objectBehaviour.ServerSetAnchored(true, interaction.Performer));
						stateful.ServerChangeState(wrenchedState);
					}
					else
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "Unable to wrench frame");
					}
				}
			}
			else if (CurrentState == wrenchedState)
			{
				if (Validations.HasUsedComponent<ComputerCircuitboard>(interaction) && circuitBoardSlot.IsEmpty)
				{
					//stick in the circuit board
					Chat.AddActionMsgToChat(interaction, $"You place the {interaction.UsedObject.ExpensiveName()} inside the frame.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.UsedObject.ExpensiveName()} inside the frame.");

					machineParts = interaction.UsedObject.GetComponent<MachineCircuitBoard>().MachinePartsUsed;
					Inventory.ServerTransfer(interaction.HandSlot, circuitBoardSlot);
					stateful.ServerChangeState(circuitAddedState);
				}
				else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					//unwrench
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start to unfasten the frame...",
						$"{interaction.Performer.ExpensiveName()} starts to unfasten the frame...",
						"You unfasten the frame.",
						$"{interaction.Performer.ExpensiveName()} unfastens the frame.",
						() => objectBehaviour.ServerSetAnchored(false, interaction.Performer));
					stateful.ServerChangeState(cablesAddedState);
				}
			}
			else if (CurrentState == circuitAddedState)
			{
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) && circuitBoardSlot.IsOccupied)
				{
					//wrench out the circuit board
					Chat.AddActionMsgToChat(interaction, $"You remove the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.",
						$"{interaction.Performer.ExpensiveName()} removes the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.");
					ToolUtils.ServerPlayToolSound(interaction);
					Inventory.ServerDrop(circuitBoardSlot);
					stateful.ServerChangeState(wrenchedState);
				}
				else if (ItemTraitCheck(interaction))
				{
					var usedObject = interaction.UsedObject;

					Chat.AddActionMsgToChat(interaction, $"You place the {usedObject.ExpensiveName()} inside the frame.",
						$"{interaction.Performer.ExpensiveName()} places the {usedObject.ExpensiveName()} inside the frame.");

					PartCheck(usedObject, interaction);

					//foreach ()
					//{

					//}

					if (partsAddedState)
					{
						stateful.ServerChangeState(partsAddedState);
					}
				}
			}


		}

		private void PartCheck(GameObject usedObject, HandApply interaction)
		{
			for(int i = 0; i < machineParts.machineParts.Length; i++)
			{
				if (usedObject.GetComponent<ItemAttributesV2>().HasTrait(machineParts.machineParts[i].itemTrait))
				{
					trait = machineParts.machineParts[i];
					break;
				}
			}

			var needed = trait.amountOfThisPart;

			var itemTrait = trait.itemTrait;

			if (partsUsed.ContainsKey(itemTrait) && usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount >= needed) //if the object already exists, and its stackable and some of it is needed.
			{
				partsUsed[itemTrait] = needed;
				usedObject.GetComponent<Stackable>().ServerConsume(needed);
			}
			else if (partsUsed.ContainsKey(itemTrait) && usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount < needed)//if the object already exists, and its stackable and all of its needed.
			{
				var used = usedObject.GetComponent<Stackable>().Amount;
				partsUsed[itemTrait] += used;
				usedObject.GetComponent<Stackable>().ServerConsume(used);
			}
			else if (usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount >= needed) //if the object doesnt exists, and its stackable and some of it is needed.
			{
				partsUsed.Add(itemTrait, needed);
				usedObject.GetComponent<Stackable>().ServerConsume(needed);
			}
			else if (usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount < needed)//if the object doesnt exists, and its stackable and all of its needed.
			{
				var used = usedObject.GetComponent<Stackable>().Amount;
				partsUsed.Add(itemTrait, used);
				usedObject.GetComponent<Stackable>().ServerConsume(used);
			}
			else if (partsUsed.ContainsKey(itemTrait))// Already exists but isnt stackable
			{
				partsUsed[itemTrait] ++;

				Inventory.ServerDespawn(interaction.HandSlot);
			}
			else// Doesnt exist but isnt stackable
			{
				partsUsed.Add(itemTrait, 1);
				Inventory.ServerDespawn(interaction.HandSlot);
			}
		}

		private bool ItemTraitCheck(HandApply interaction)
		{
			foreach (var part in machineParts.machineParts)
			{
				if (Validations.HasUsedItemTrait(interaction, part.itemTrait))
				{
					return true;
				}
			}

			return false;
		}

		public string Examine(Vector3 worldPos)
		{
			string msg = "";
			if (CurrentState == initialState)
			{
				if (objectBehaviour.IsPushable)
				{
					msg = "Use a wrench to secure the frame to the floor, or a welder to deconstruct it.";
				}

				else
				{
					msg = "Use a wrench to unfasten the frame from the floor.";
					if (circuitBoardSlot.IsEmpty)
					{
						msg += " A circuit board must be added to continue.";
					}

					if (circuitBoardSlot.IsOccupied)
					{
						msg += " Use a screwdriver to screw in the circuitboard, or a crowbar to remove it.";
					}
				}
			}

			if (CurrentState == initialState)
			{
				msg = " Add five wires must be added to continue construction. Use a screwdriver to unfasten the circuitboard.";
			}


			if (CurrentState == cablesAddedState)
			{
				msg = "Add two glass sheets to mount the glass panel. Use a wirecutter to remove cables.";
			}

			if (CurrentState == initialState)
			{
				msg = "Connect the monitor with a screwdriver to finish construction. Use a crowbar to remove glass panel.";
			}

			return msg;
		}

		/// <summary>
		/// Initializes this frame's state to be from a just-deconstructed computer
		/// </summary>
		/// <param name="computer"></param>
		public void ServerInitFromComputer(Computer computer)
		{
			//create the circuit board
			var board = Spawn.ServerPrefab(computer.CircuitBoardPrefab);
			//put it in
			Inventory.ServerAdd(board.GameObject, circuitBoardSlot);

			//set initial state
			objectBehaviour.ServerSetPushable(false);
			stateful.ServerChangeState(initialState);
		}
	}
}