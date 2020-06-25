
using System;
using Construction;
using UnityEngine;

/// <summary>
/// Main component for computer construction (ComputerFrame).
/// </summary>
public class ComputerFrame : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
{

	[SerializeField] private StatefulState initialState = null;
	[SerializeField] private StatefulState cablesAddedState = null;
	[SerializeField] private StatefulState circuitScrewedState = null;
	[SerializeField] private StatefulState glassAddedState = null;

	private ItemSlot circuitBoardSlot;
	private Stateful stateful;
	private StatefulState CurrentState => stateful.CurrentState;
	private ObjectBehaviour objectBehaviour;
	private Integrity integrity;

	private void Awake()
	{
		circuitBoardSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
		stateful = GetComponent<Stateful>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

		if (!CustomNetworkManager.IsServer) return;

		integrity = GetComponent<Integrity>();

		integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		//different logic depending on state
		if (CurrentState == initialState)
		{
			if (objectBehaviour.IsPushable)
			{
				//wrench in place or deconstruct
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench) ||
					Validations.HasUsedActiveWelder(interaction);
			}

			//insert, unwrench, or screw  or pry out circuitboard (client can't see this storage inventory so we can't check the slot contents clientside
			return Validations.HasUsedComponent<ComputerCircuitboard>(interaction) ||
			       Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
			       Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) ||
			       Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench);
		}
		else if (CurrentState == circuitScrewedState)
		{
			//unscrew circuit board or add 5 cables
			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
			       (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) && Validations.HasUsedAtLeast(interaction, 5));
		}
		else if (CurrentState == cablesAddedState)
		{
			//add glass or cut out cables
			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter) ||
			       (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.GlassSheet) && Validations.HasUsedAtLeast(interaction, 2));
		}
		else if (CurrentState == glassAddedState)
		{
			//screw in monitor or pry off glass
			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
			       Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar);
		}

		return false;

	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (CurrentState == initialState)
		{
			if (objectBehaviour.IsPushable)
			{
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
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
					}
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
			else
			{
				//already wrenched in place
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					//unwrench
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start to unfasten the frame...",
						$"{interaction.Performer.ExpensiveName()} starts to unfasten the frame...",
						"You unfasten the frame.",
						$"{interaction.Performer.ExpensiveName()} unfastens the frame.",
						() => objectBehaviour.ServerSetAnchored(false, interaction.Performer));
				}
				else if (Validations.HasUsedComponent<ComputerCircuitboard>(interaction) && circuitBoardSlot.IsEmpty)
				{
					//stick in the circuit board
					Chat.AddActionMsgToChat(interaction, $"You place the {interaction.UsedObject.ExpensiveName()} inside the frame.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.UsedObject.ExpensiveName()} inside the frame.");
					Inventory.ServerTransfer(interaction.HandSlot, circuitBoardSlot);
				}
				else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) && circuitBoardSlot.IsOccupied)
				{
					//screw in the circuit board
					Chat.AddActionMsgToChat(interaction, $"You screw {circuitBoardSlot.ItemObject.ExpensiveName()} into place.",
						$"{interaction.Performer.ExpensiveName()} screws {circuitBoardSlot.ItemObject.ExpensiveName()} into place.");
					ToolUtils.ServerPlayToolSound(interaction);
					stateful.ServerChangeState(circuitScrewedState);
				}
				else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) &&
				         circuitBoardSlot.IsOccupied)
				{
					//wrench out the circuit board
					Chat.AddActionMsgToChat(interaction, $"You remove the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.",
						$"{interaction.Performer.ExpensiveName()} removes the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.");
					ToolUtils.ServerPlayToolSound(interaction);
					Inventory.ServerDrop(circuitBoardSlot);
				}
			}
		}
		else if (CurrentState == circuitScrewedState)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				//unscrew circuit board
				Chat.AddActionMsgToChat(interaction, $"You unfasten the circuit board.",
					$"{interaction.Performer.ExpensiveName()} unfastens the circuit board.");
				ToolUtils.ServerPlayToolSound(interaction);
				stateful.ServerChangeState(initialState);
			}
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) &&
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
				stateful.ServerChangeState(circuitScrewedState);
			}
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.GlassSheet) &&
			         Validations.HasUsedAtLeast(interaction, 2))
			{
				//add glass
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to put in the glass panel...",
					$"{interaction.Performer.ExpensiveName()} starts to put in the glass panel...",
					"You put in the glass panel.",
					$"{interaction.Performer.ExpensiveName()} puts in the glass panel.",
					() =>
					{
						Inventory.ServerConsume(interaction.HandSlot, 2);
						stateful.ServerChangeState(glassAddedState);
					});
			}
		}
		else if (CurrentState == glassAddedState)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				//screw in monitor, completing construction
				Chat.AddActionMsgToChat(interaction, $"You connect the monitor.",
					$"{interaction.Performer.ExpensiveName()} connects the monitor.");
				ToolUtils.ServerPlayToolSound(interaction);
				var circuitBoard = circuitBoardSlot.ItemObject?.GetComponent<ComputerCircuitboard>();
				if (circuitBoard == null)
				{
					Logger.LogWarningFormat("Cannot complete computer, circuit board not in frame {0}. Probably a coding error.",
						Category.Interaction, name);
					return;
				}
				Spawn.ServerPrefab(circuitBoard.ComputerToSpawn, SpawnDestination.At(gameObject));
				Despawn.ServerSingle(gameObject);
			}
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar))
			{
				//remove glass
				Chat.AddActionMsgToChat(interaction, $"You remove the glass panel.",
					$"{interaction.Performer.ExpensiveName()} removes the glass panel.");
				ToolUtils.ServerPlayToolSound(interaction);
				Spawn.ServerPrefab(CommonPrefabs.Instance.GlassSheet, SpawnDestination.At(gameObject), 2);
				stateful.ServerChangeState(cablesAddedState);
			}
		}
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

		if (CurrentState == circuitScrewedState)
		{
			msg = " Add five wires must be added to continue construction. Use a screwdriver to unfasten the circuitboard.";
		}


		if (CurrentState == cablesAddedState)
		{
			msg = "Add two glass sheets to mount the glass panel. Use a wirecutter to remove cables.";
		}

		if (CurrentState == glassAddedState)
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
		stateful.ServerChangeState(glassAddedState);
	}

	public void WhenDestroyed(DestructionInfo info)
	{
		if (circuitBoardSlot.IsOccupied)
		{
			Inventory.ServerDrop(circuitBoardSlot);
		}

		if (CurrentState == glassAddedState)
		{
			Spawn.ServerPrefab(CommonPrefabs.Instance.GlassSheet, SpawnDestination.At(gameObject), UnityEngine.Random.Range(1, 2));
			Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), UnityEngine.Random.Range(1, 5));
		}

		if (CurrentState == cablesAddedState)
		{
			Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), UnityEngine.Random.Range(1, 5));
		}

		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), UnityEngine.Random.Range(1,5));

		integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
	}
}
