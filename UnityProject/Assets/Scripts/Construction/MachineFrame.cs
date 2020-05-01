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
		private IDictionary<ItemTrait, int> basicPartsUsed = new Dictionary<ItemTrait, int>();
		private IDictionary<ItemTrait, int> partsUsed = new Dictionary<ItemTrait, int>();
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
				Logger.Log("inital state");
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
					Validations.HasUsedComponent<MachineCircuitBoard>(interaction);
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
			Logger.Log("No interact");
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
				if (Validations.HasUsedComponent<MachineCircuitBoard>(interaction) && circuitBoardSlot.IsEmpty)
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
					//TO DO RESPAWN ALREADY PUT IN ITEMS

					foreach (var item in partsUsed)
					{
						if (StockPartsCheck(item.Key, interaction) == item.Key)
						{
							for (int i = 0; i <= item.Value; i++)
							{
								Spawn.ServerPrefab(StockPartsPrefabCheck(item.Key), SpawnDestination.At(gameObject));
							}
						}
					}
				}
				else if (ItemTraitCheck(interaction))
				{
					var usedObject = interaction.UsedObject;

					Chat.AddActionMsgToChat(interaction, $"You place the {usedObject.ExpensiveName()} inside the frame.",
						$"{interaction.Performer.ExpensiveName()} places the {usedObject.ExpensiveName()} inside the frame.");

					PartCheck(usedObject, interaction);

					foreach (var parts in machineParts.machineParts)
					{
						if (!basicPartsUsed.ContainsKey(parts.itemTrait)) return;

						if (basicPartsUsed[parts.itemTrait] != parts.amountOfThisPart)
						{
							return;
						}
					}

					stateful.ServerChangeState(partsAddedState);
				}
			}
			else if (CurrentState == partsAddedState)
			{
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					Spawn.ServerPrefab(machineParts.machine, SpawnDestination.At(gameObject));
					Despawn.ServerSingle(gameObject);
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

			if (basicPartsUsed.ContainsKey(itemTrait) && usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount >= needed) //if the object already exists, and its stackable and some of it is needed.
			{
				basicPartsUsed[itemTrait] = needed;
				usedObject.GetComponent<Stackable>().ServerConsume(needed);
			}
			else if (basicPartsUsed.ContainsKey(itemTrait) && usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount < needed)//if the object already exists, and its stackable and all of its needed.
			{
				var used = usedObject.GetComponent<Stackable>().Amount;
				basicPartsUsed[itemTrait] += used;
				usedObject.GetComponent<Stackable>().ServerConsume(used);
			}
			else if (usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount >= needed) //if the object doesnt exists, and its stackable and some of it is needed.
			{
				basicPartsUsed.Add(itemTrait, needed);
				usedObject.GetComponent<Stackable>().ServerConsume(needed);
			}
			else if (usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount < needed)//if the object doesnt exists, and its stackable and all of its needed.
			{
				var used = usedObject.GetComponent<Stackable>().Amount;
				basicPartsUsed.Add(itemTrait, used);
				usedObject.GetComponent<Stackable>().ServerConsume(used);
			}
			else if (basicPartsUsed.ContainsKey(itemTrait))// Already exists but isnt stackable
			{
				basicPartsUsed[itemTrait] ++;
				partsUsed[StockPartsCheck(itemTrait, interaction)]++;
				Inventory.ServerDespawn(interaction.HandSlot);
			}
			else// Doesnt exist but isnt stackable
			{
				basicPartsUsed.Add(itemTrait, 1);
				partsUsed.Add(StockPartsCheck(itemTrait, interaction), 1);
				Inventory.ServerDespawn(interaction.HandSlot);
			}
		}

		private bool ItemTraitCheck(HandApply interaction)
		{
			foreach (var part in machineParts.machineParts)
			{
				if (Validations.HasUsedItemTrait(interaction, part.itemTrait) && (!basicPartsUsed.ContainsKey(part.itemTrait) || basicPartsUsed[part.itemTrait] != part.amountOfThisPart))
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
				msg = " Add five wires must be added to continue construction.";
			}

			if (CurrentState == cablesAddedState)
			{
				msg = " Wrench down the frame to continue construction. Or use a wirecutter to remove the cables.";
			}


			if (CurrentState == wrenchedState)
			{
				msg = "Add a machine circuit to continue construction. Or wrench to unanchor the frame.";
			}

			if (CurrentState == circuitAddedState)
			{
				msg = "You have these items left to add: \n";

				foreach (var parts in machineParts.machineParts)
				{
					if (!basicPartsUsed.TryGetValue(parts.itemTrait, out int value))
					{
						msg += parts.amountOfThisPart;
						msg += " " + parts.itemTrait;

						if (parts.amountOfThisPart > 1)
						{
							msg += "s";
						}

						msg += "\n";
					}
					else if (basicPartsUsed[parts.itemTrait] != parts.amountOfThisPart)
					{
						msg += parts.amountOfThisPart - basicPartsUsed[parts.itemTrait];
						msg += " " + parts.itemTrait;

						if ((parts.amountOfThisPart - basicPartsUsed[parts.itemTrait]) > 1)
						{
							msg += "s";
						}

						msg += "\n";
					}
				}

				msg += "Use wrench to remove circuit board, you will destroy all non stackable items.";
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


		public GameObject StockPartsPrefabCheck(ItemTrait itemTrait)
		{
			if (PrefabManipulator(itemTrait) != null)
			{
				return PrefabManipulator(itemTrait);
			}
			else if (PrefabMatterBin(itemTrait) != null)
			{
				return PrefabMatterBin(itemTrait);
			}
			else if (PrefabMatterBin(itemTrait) != null)
			{
				return PrefabMicroLaser(itemTrait);
			}
			else if (PrefabMatterBin(itemTrait) != null)
			{
				return PrefabScanningModule(itemTrait);
			}
			else if (PrefabMatterBin(itemTrait) != null)
			{
				return PrefabCapacitor(itemTrait);
			}

			Logger.LogError("StockPartPrefabCheck Null error, this shouldnt happen");
			return null;
		}

		private GameObject PrefabManipulator(ItemTrait itemTrait)
		{
			if (itemTrait == MachinePartsItemTraits.Instance.MicroManipulator)
			{
				return MachinePartsPrefabs.Instance.MicroManipulator;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.NanoManipulator)
			{
				return MachinePartsPrefabs.Instance.NanoManipulator;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.PicoManipulator)
			{
				return MachinePartsPrefabs.Instance.PicoManipulator;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.FemtoManipulator)
			{
				return MachinePartsPrefabs.Instance.FemtoManipulator;
			}

			Logger.LogError("machineframe.cs PrefabManipulator() has returned null, this shouldnt happen");
			return null;
		}

		private GameObject PrefabMatterBin(ItemTrait itemTrait)
		{
			if (itemTrait == MachinePartsItemTraits.Instance.BasicMatterBin)
			{
				return MachinePartsPrefabs.Instance.BasicMatterBin;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.AdvancedMatterBin)
			{
				return MachinePartsPrefabs.Instance.AdvancedMatterBin;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.SuperMatterBin)
			{
				return MachinePartsPrefabs.Instance.SuperMatterBin;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.BluespaceMatterBin)
			{
				return MachinePartsPrefabs.Instance.BluespaceMatterBin;
			}
			Logger.LogError("machineframe.cs PrefabMatterBin() has returned null, this shouldnt happen");
			return null;
		}

		private GameObject PrefabMicroLaser(ItemTrait itemTrait)
		{
			if (itemTrait == MachinePartsItemTraits.Instance.BasicMicroLaser)
			{
				return MachinePartsPrefabs.Instance.BasicMicroLaser;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.HighPowerMicroLaser)
			{
				return MachinePartsPrefabs.Instance.HighPowerMicroLaser;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.UltraHighPowerMicroLaser)
			{
				return MachinePartsPrefabs.Instance.UltraHighPowerMicroLaser;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.QuadUltraMicroLaser)
			{
				return MachinePartsPrefabs.Instance.QuadUltraMicroLaser;
			}
			Logger.LogError("machineframe.cs PrefabMicroLaser() has returned null, this shouldnt happen");
			return null;
		}

		private GameObject PrefabScanningModule(ItemTrait itemTrait)
		{
			if (itemTrait == MachinePartsItemTraits.Instance.BasicScanningModule)
			{
				return MachinePartsPrefabs.Instance.BasicScanningModule;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.AdvancedScanningModule)
			{
				return MachinePartsPrefabs.Instance.AdvancedScanningModule;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.PhasicScanningModule)
			{
				return MachinePartsPrefabs.Instance.PhasicScanningModule;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.TriphasicScanningModule)
			{
				return MachinePartsPrefabs.Instance.TriphasicScanningModule;
			}
			Logger.LogError("machineframe.cs PrefabScanningModule() has returned null, this shouldnt happen");
			return null;
		}

		private GameObject PrefabCapacitor(ItemTrait itemTrait)
		{
			if (itemTrait == MachinePartsItemTraits.Instance.BasicCapacitor)
			{
				return MachinePartsPrefabs.Instance.BasicCapacitor;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.AdvancedCapacitor)
			{
				return MachinePartsPrefabs.Instance.AdvancedCapacitor;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.SuperCapacitor)
			{
				return MachinePartsPrefabs.Instance.SuperCapacitor;
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.QuadraticCapacitor)
			{
				return MachinePartsPrefabs.Instance.QuadraticCapacitor;
			}
			Logger.LogError("machineframe.cs PrefabCapacitor() has returned null, this shouldnt happen");
			return null;
		}

		public ItemTrait StockPartsCheck(ItemTrait itemTrait, HandApply interaction)
		{
			if (itemTrait == MachinePartsItemTraits.Instance.Manipulator)
			{
				return Manipulator(interaction);
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.MatterBin)
			{
				return MatterBin(interaction);
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.MicroLaser)
			{
				return MicroLaser(interaction);
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.ScanningModule)
			{
				return ScanningModule(interaction);
			}
			else if (itemTrait == MachinePartsItemTraits.Instance.Capacitor)
			{
				return Capacitor(interaction);
			}

			return itemTrait;
		}

		private ItemTrait Manipulator(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.MicroManipulator))
			{
				return MachinePartsItemTraits.Instance.MicroManipulator;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.NanoManipulator))
			{
				return MachinePartsItemTraits.Instance.NanoManipulator;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.PicoManipulator))
			{
				return MachinePartsItemTraits.Instance.PicoManipulator;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.FemtoManipulator))
			{
				return MachinePartsItemTraits.Instance.FemtoManipulator;
			}
			Logger.LogError("machineframe.cs Manipulator() has returned null, this shouldnt happen");
			return null;
		}

		private ItemTrait MatterBin(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.BasicMatterBin))
			{
				return MachinePartsItemTraits.Instance.BasicMatterBin;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.AdvancedMatterBin))
			{
				return MachinePartsItemTraits.Instance.AdvancedMatterBin;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.SuperMatterBin))
			{
				return MachinePartsItemTraits.Instance.SuperMatterBin;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.BluespaceMatterBin))
			{
				return MachinePartsItemTraits.Instance.BluespaceMatterBin;
			}
			Logger.LogError("machineframe.cs MatterBin() has returned null, this shouldnt happen");
			return null;
		}

		private ItemTrait MicroLaser(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.BasicMicroLaser))
			{
				return MachinePartsItemTraits.Instance.BasicMicroLaser;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.HighPowerMicroLaser))
			{
				return MachinePartsItemTraits.Instance.HighPowerMicroLaser;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.UltraHighPowerMicroLaser))
			{
				return MachinePartsItemTraits.Instance.UltraHighPowerMicroLaser;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.QuadUltraMicroLaser))
			{
				return MachinePartsItemTraits.Instance.QuadUltraMicroLaser;
			}
			Logger.LogError("machineframe.cs MicroLaser() has returned null, this shouldnt happen");
			return null;
		}

		private ItemTrait ScanningModule(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.BasicScanningModule))
			{
				return MachinePartsItemTraits.Instance.BasicScanningModule;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.AdvancedScanningModule))
			{
				return MachinePartsItemTraits.Instance.AdvancedScanningModule;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.PhasicScanningModule))
			{
				return MachinePartsItemTraits.Instance.PhasicScanningModule;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.TriphasicScanningModule))
			{
				return MachinePartsItemTraits.Instance.TriphasicScanningModule;
			}
			Logger.LogError("machineframe.cs ScanningModule() has returned null, this shouldnt happen");
			return null;
		}

		private ItemTrait Capacitor(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.BasicCapacitor))
			{
				return MachinePartsItemTraits.Instance.BasicCapacitor;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.AdvancedCapacitor))
			{
				return MachinePartsItemTraits.Instance.AdvancedCapacitor;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.SuperCapacitor))
			{
				return MachinePartsItemTraits.Instance.SuperCapacitor;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, MachinePartsItemTraits.Instance.QuadraticCapacitor))
			{
				return MachinePartsItemTraits.Instance.QuadraticCapacitor;
			}
			Logger.LogError("machineframe.cs Capacitor() has returned null, this shouldnt happen");
			return null;
		}
	}
}