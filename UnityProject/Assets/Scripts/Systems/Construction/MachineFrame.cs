using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Logs;
using UnityEngine;
using Mirror;
using ScriptableObjects;
using Machines;
using Objects.Machines;

namespace Objects.Construction
{
	[Serializable]
	public class AllowedTraitList
	{
		public AllowedTraitList()
		{

		}
		public AllowedTraitList(ItemTrait allowedTrait)
		{
			AllowedTrait = allowedTrait;
		}

		public ItemTrait AllowedTrait;
	}

	[Serializable]
	public class SyncListItem : SyncList<AllowedTraitList> { }

	/// <summary>
	/// Main Component for Machine Construction
	/// </summary>
	public class MachineFrame : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState cablesAddedState = null;
		[SerializeField] private StatefulState wrenchedState = null;
		[SerializeField] private StatefulState circuitAddedState = null;
		[SerializeField] private StatefulState partsAddedState = null;

		private UniversalObjectPhysics objectBehaviour;
		private Integrity integrity;
		private SpriteHandler spriteHandler;

		private ItemSlot circuitBoardSlot;//Index 0
		private IDictionary<ItemTrait, int> basicPartsUsed = new Dictionary<ItemTrait, int>();
		private IDictionary<GameObject, int> partsInFrame = new Dictionary<GameObject, int>();
		private Stateful stateful;

		private MachineParts machineParts;

		private readonly SyncListItem allowedTraits = new SyncListItem();

		private List<AllowedTraitList> listOfAllowedTraits = new List<AllowedTraitList>();

		private MachineParts.MachinePartList machinePartsList;

		private bool putBoardInManually;

		private StatefulState CurrentState => stateful.CurrentState;

		private List<VendorItem> previousVendorContent;

		private void Awake()
		{
			circuitBoardSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();

			if (!CustomNetworkManager.IsServer) return;

			integrity = GetComponent<Integrity>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();

			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);

			if (CurrentState != partsAddedState)
			{
				stateful.ServerChangeState(initialState);
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			//tracks change for client
			allowedTraits.Callback += AllowedTraitsCallBack;
		}

		/// <summary>
		/// Client calls this when the list is changed
		/// </summary>
		/// <param name="op"></param>
		/// <param name="itemIndex"></param>
		/// <param name="oldItem"></param>
		/// <param name="newItem"></param>
		private void AllowedTraitsCallBack(SyncListItem.Operation op, int itemIndex, AllowedTraitList oldItem, AllowedTraitList newItem)
		{
			switch (op)
			{
				case SyncListItem.Operation.OP_ADD:
					listOfAllowedTraits.Add(newItem);
					break;
				case SyncListItem.Operation.OP_CLEAR:
					listOfAllowedTraits.Clear();
					break;
				case SyncListItem.Operation.OP_INSERT:
					break;
				case SyncListItem.Operation.OP_REMOVEAT:
					break;
				case SyncListItem.Operation.OP_SET:
					break;
				default:
					break;
			}
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
				//Add 5 cables or deconstruct
				return (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) && Validations.HasUsedAtLeast(interaction, 5)) ||
					Validations.HasUsedActiveWelder(interaction);
			}
			else if (CurrentState == cablesAddedState)
			{
				//cut cables or wrench frame
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter) ||
					  Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench);
			}
			else if (CurrentState == wrenchedState)
			{
				//Unwrench or add circuit board
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) ||
					Validations.HasUsedComponent<MachineCircuitBoard>(interaction);
			}
			else if (CurrentState == circuitAddedState)
			{
				//remove circuit board, also removes all parts that have been added
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
				{
					return true;
				}

				//check part item traits, if in scriptableObject of the machine then return true.
				foreach (var part in listOfAllowedTraits)
				{
					if (Validations.HasItemTrait(interaction, part.AllowedTrait))
					{
						return true;
					}
				}
				return true;
			}
			else if (CurrentState == partsAddedState)
			{
				//screw in parts or crowbar out circuit board which removes all parts
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
			else if (CurrentState == wrenchedState)
			{
				WrenchedStateInteraction(interaction);
			}
			else if (CurrentState == circuitAddedState)
			{
				CircuitAddedStateInteraction(interaction);
			}
			else if (CurrentState == partsAddedState)
			{
				PartsAddedStateInteraction(interaction);
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

						spriteHandler.ChangeSprite((int) SpriteStates.BoxCable);
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
						_ = Despawn.ServerSingle(gameObject);
					});
			}
		}

		/// <summary>
		/// Stage 2, Wrench down to continue construction, anchors machine, or wirecutters to move to stage 1
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

				spriteHandler.ChangeSprite((int)SpriteStates.Box);
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
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

		/// <summary>
		/// Stage 3, add circuit board which contains data for construction, or use wrench to go back to stage 2
		/// </summary>
		/// <param name="interaction"></param>
		private void WrenchedStateInteraction(HandApply interaction)
		{
			if (Validations.HasUsedComponent<MachineCircuitBoard>(interaction) && circuitBoardSlot.IsEmpty)
			{
				//Transfer parts data
				machineParts = interaction.UsedObject.GetComponent<MachineCircuitBoard>().MachinePartsUsed;

				if (machineParts == null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "The machine circuit board is not specialised.");
					return;
				}

				//stick in the circuit board
				Chat.AddActionMsgToChat(interaction, $"You place the {interaction.UsedObject.ExpensiveName()} inside the frame.",
					$"{interaction.Performer.ExpensiveName()} places the {interaction.UsedObject.ExpensiveName()} inside the frame.");

				//Syncing allowed traits to clients
				allowedTraits.Clear();

				foreach (var list in machineParts.machineParts)
				{
					allowedTraits.Add(new AllowedTraitList(list.itemTrait));
				}

				netIdentity.isDirty = true;

				Inventory.ServerTransfer(interaction.HandSlot, circuitBoardSlot);
				stateful.ServerChangeState(circuitAddedState);
				putBoardInManually = true;

				spriteHandler.ChangeSprite((int)SpriteStates.BoxCircuit);
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
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

		/// <summary>
		/// Stage 4, Add in valid parts, or remove circuit board to go back to stage 3.
		/// </summary>
		/// <param name="interaction"></param>
		private void CircuitAddedStateInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) && circuitBoardSlot.IsOccupied)
			{
				//wrench out the circuit board, when it only has some of the parts
				Chat.AddActionMsgToChat(interaction, $"You remove the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.",
					$"{interaction.Performer.ExpensiveName()} removes the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.");
				ToolUtils.ServerPlayToolSound(interaction);

				RemoveCircuitAndParts();
			}
			else if (ItemTraitCheck(interaction)) //Adding parts validation
			{
				var usedObject = interaction.UsedObject;

				Chat.AddActionMsgToChat(interaction, $"You place the {usedObject.ExpensiveName()} inside the frame.",
					$"{interaction.Performer.ExpensiveName()} places the {usedObject.ExpensiveName()} inside the frame.");

				//Process Part
				PartCheck(usedObject, interaction);

				//Check we have all the parts so we can move on to next stage.
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

		/// <summary>
		/// Stage 5, complete construction, or remove board and return to stage 3.
		/// </summary>
		/// <param name="interaction"></param>
		private void PartsAddedStateInteraction(HandApply interaction)
		{
			//Complete construction, spawn new machine and send data over to it.
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				var spawnedObject = Spawn.ServerPrefab(machineParts.machine, SpawnDestination.At(gameObject)).GameObject.GetComponent<Machine>();

				if (spawnedObject == null)
				{
					Loggy.LogWarning(machineParts.machine + " is missing the machine script!", Category.Construction);
					return;
				}

				//Send circuit board data to the new machine
				spawnedObject.SetMachineParts(machineParts);
				spawnedObject.SetPartsInFrame(partsInFrame);


				//Restoring previous vendor content if possible
				var vendor = spawnedObject.GetComponent<Vendor>();
				if (vendor != null)
				{
					var vendorContent = GetPreviousVendorContent();
					if(vendorContent != null)
					{
						vendor.VendorContent = vendorContent;
					}
				}

				//Despawn frame
				_ = Despawn.ServerSingle(gameObject);
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) && circuitBoardSlot.IsOccupied)
			{
				//wrench out the circuit board, when it has all the parts in.
				Chat.AddActionMsgToChat(interaction, $"You remove the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.",
					$"{interaction.Performer.ExpensiveName()} removes the {circuitBoardSlot.ItemObject.ExpensiveName()} from the frame.");
				ToolUtils.ServerPlayToolSound(interaction);

				RemoveCircuitAndParts();
			}
		}

		/// <summary>
		/// Function to process the part which has been applied to the frame
		/// </summary>
		/// <param name="usedObject"></param>
		/// <param name="interaction"></param>
		private void PartCheck(GameObject usedObject, HandApply interaction)
		{
			// For all the list of data(itemtraits, amounts needed) in machine parts
			for(int i = 0; i < machineParts.machineParts.Length; i++)
			{
				// If the interaction object has an itemtrait thats in the list, set the list machinePartsList variable as the list from the machineParts data from the circuit board.
				if (usedObject.GetComponent<ItemAttributesV2>().HasTrait(machineParts.machineParts[i].itemTrait))
				{
					machinePartsList = machineParts.machineParts[i];
					break;

					// IF YOU WANT AN ITEM TO HAVE TWO ITEMTTRAITS WHICH CONTRIBUTE TO THE MACHINE BUILIDNG PROCESS, THIS NEEDS TO BE REFACTORED
					// all the stuff below needs to go into its own method which gets called here, replace the break;
				}
			}

			// Amount of the itemtrait that is needed for the machine to be buildable
			var needed = machinePartsList.amountOfThisPart;

			// Itemtrait currently being looked at.
			var itemTrait = machinePartsList.itemTrait;

			// If theres already the itemtrait how many more do we need
			if (basicPartsUsed.ContainsKey(itemTrait))
			{
				needed -= basicPartsUsed[itemTrait];
			}

			//Main logic for tallying up and moving parts to hidden pos
			if (basicPartsUsed.ContainsKey(itemTrait) && usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount >= needed) //if the itemTrait already exists, and its stackable and some of it is needed.
			{
				var StackingItem = usedObject.GetComponent<Stackable>();
				var oldAmount = StackingItem.Amount;
				var addNew = StackingItem.ServerRemoveOne();
				addNew.GetComponent<Stackable>().ServerSetAmount(needed);

				StackingItem.ServerSetAmount(oldAmount -needed);

				basicPartsUsed[itemTrait] = machinePartsList.amountOfThisPart;
				AddItemToDict(addNew, needed, interaction);
			}
			else if (basicPartsUsed.ContainsKey(itemTrait) && usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount < needed)//if the itemTrait already exists, and its stackable and all of its needed.
			{
				var used = usedObject.GetComponent<Stackable>().Amount;
				basicPartsUsed[itemTrait] += used;

				Inventory.ServerDrop(interaction.HandSlot);

				AddItemToDict(usedObject, used, interaction);

			}
			else if (usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount >= needed) //if the itemTrait doesnt exists, and its stackable and some of it is needed.
			{
				var StackingItem = usedObject.GetComponent<Stackable>();
				var oldAmount = StackingItem.Amount;
				var addNew = StackingItem.ServerRemoveOne();
				addNew.GetComponent<Stackable>().ServerSetAmount(needed);

				StackingItem.ServerSetAmount(oldAmount -needed );
				basicPartsUsed.Add(itemTrait, needed);

				AddItemToDict(addNew, needed, interaction);

			}
			else if (usedObject.GetComponent<Stackable>() != null && usedObject.GetComponent<Stackable>().Amount < needed)//if the itemTrait doesnt exists, and its stackable and all of its needed.
			{
				var used = usedObject.GetComponent<Stackable>().Amount;
				basicPartsUsed.Add(itemTrait, used);

				Inventory.ServerDrop(interaction.HandSlot);

				AddItemToDict(usedObject, used, interaction);
			}
			else if (basicPartsUsed.ContainsKey(itemTrait))// ItemTrait already exists but isnt stackable
			{
				basicPartsUsed[itemTrait] ++;

				Inventory.ServerDrop(interaction.HandSlot);

				AddItemToDict(usedObject, 1, interaction);
			}
			else// ItemTrait doesnt exist but isnt stackable
			{
				basicPartsUsed.Add(itemTrait, 1);

				Inventory.ServerDrop(interaction.HandSlot);

				AddItemToDict(usedObject, 1, interaction);
			}
		}

		/// <summary>
		/// Adds the part object to the dictionaries and moves items to hidden pos
		/// </summary>
		/// <param name="usedObject"></param>
		/// <param name="amount"></param>
		/// <param name="interaction"></param>
		private void AddItemToDict(GameObject usedObject, int amount, HandApply interaction)
		{
			// If its stackable, make copy itself, set amount used, send to hidden pos.
			if (usedObject.GetComponent<Stackable>() != null)
			{
				// Returns usedObject if stack amount is 1, if > 1 then creates new object.
				var newObject = usedObject.GetComponent<Stackable>().ServerRemoveOne();

				//If a new object was created
				if (newObject != usedObject)
				{
					usedObject.GetComponent<Stackable>().ServerConsume(amount - 1);

					newObject.GetComponent<Stackable>().ServerIncrease(amount - 1);

					if (usedObject.GetComponent<Stackable>().Amount != 0)
					{
						Inventory.ServerAdd(usedObject, interaction.HandSlot);
					}
				}
				else if (newObject.GetComponent<Stackable>().Amount == 0)
				{
					// Sets old objects amount if amount is 0
					newObject.GetComponent<Stackable>().ServerIncrease(amount);
				}

				newObject.GetComponent<UniversalObjectPhysics>().DisappearFromWorld();

				if (newObject.transform.parent != gameObject.transform.parent)
				{
					newObject.transform.parent = gameObject.transform.parent;
				}

				partsInFrame.Add(newObject, amount);
			}
			// If not stackable send to hidden pos
			else
			{
				usedObject.GetComponent<UniversalObjectPhysics>().DisappearFromWorld();

				if (usedObject.transform.parent != gameObject.transform.parent)
				{
					usedObject.transform.parent = gameObject.transform.parent;
				}

				partsInFrame.Add(usedObject, amount);
			}
		}

		/// <summary>
		/// Used to validate the interaction for the server.
		/// </summary>
		/// <param name="interaction"></param>
		/// <returns></returns>
		private bool ItemTraitCheck(HandApply interaction)
		{
			foreach (var part in machineParts.machineParts)
			{
				if (Validations.HasItemTrait(interaction, part.itemTrait) && (!basicPartsUsed.ContainsKey(part.itemTrait) || basicPartsUsed[part.itemTrait] != part.amountOfThisPart)) // Has items trait and we dont have enough yet
				{
					return true;
				}
			}

			return false;
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
				msg = " Wrench down the frame to continue construction. Or use a wirecutter to remove the cables.\n";
			}


			if (CurrentState == wrenchedState)
			{
				msg = "Add a machine circuit to continue construction. Or wrench to unanchor the frame.\n";
			}

			if (CurrentState == circuitAddedState)
			{
				msg = "You have these items left to add: \n";

				foreach (var parts in machineParts.machineParts)
				{
					if (!basicPartsUsed.ContainsKey(parts.itemTrait))//If false then we have none of the itemtrait
					{
						msg += parts.amountOfThisPart;
						msg += " " + parts.itemTrait.name;

						if (parts.amountOfThisPart > 1)
						{
							msg += "s";
						}

						msg += "\n";
					}
					else if (basicPartsUsed[parts.itemTrait] != parts.amountOfThisPart)//If we have some but not enough of the itemtrait
					{
						msg += parts.amountOfThisPart - basicPartsUsed[parts.itemTrait];
						msg += " " + parts.itemTrait.name;

						if ((parts.amountOfThisPart - basicPartsUsed[parts.itemTrait]) > 1)
						{
							msg += "s";
						}

						msg += "\n";
					}
				}

				msg += "Use crowbar to remove circuit board.\n";
			}

			if (CurrentState == partsAddedState)
			{
				msg = "Use a screwdriver to finish construction or use crowbar to remove circuit board.\n";
			}

			return msg;
		}

		/// <summary>
		/// Initializes this frame's state to be from a just-deconstructed machine
		/// </summary>
		/// <param name="machine"></param>
		public void ServerInitFromComputer(Machine machine)
		{
			spriteHandler.ChangeSprite((int) SpriteStates.BoxCircuit);

			// Create the circuit board
			var board = Spawn.ServerPrefab(machine.MachineBoardPrefab).GameObject;

			if (board == null)
			{
				Loggy.LogWarning("MachineBoardPrefab was null", Category.Construction);
				return;
			}

			board.GetComponent<MachineCircuitBoard>().SetMachineParts(machine.MachineParts); // Basic item requirements to the circuit board

			//PM: Below is commented out because I've decided to make all the machines use appropriate machine board .prefabs instead of the blank board.
			/*
			board.GetComponent<ItemAttributesV2>().ServerSetArticleName(machine.MachineParts.NameOfCircuitBoard); // Sets name of board

			board.GetComponent<ItemAttributesV2>().ServerSetArticleDescription(machine.MachineParts.DescriptionOfCircuitBoard); // Sets desc of board
			*/

			// Basic items to the machine frame from the despawned machine
			machineParts = machine.MachineParts;
			partsInFrame = machine.ActiveGameObjectpartsInFrame;

			// Save vendor content if necessary, which is stored temporarily in the machine frame and transferred to the restock item if it exists
			var vendor = machine.GetComponent<Vendor>();
			if (vendor != null)
			{
				previousVendorContent = vendor.VendorContent;
			}
			TryTransferVendorContent();

			allowedTraits.Clear();

			if (machineParts == null || machineParts.machineParts == null)
			{
				Loggy.LogError($"Failed to find machine parts for {machineParts.OrNull()?.name ?? board.ExpensiveName()}");
			}
			else
			{
				foreach (var list in machineParts.machineParts)
				{
					allowedTraits.Add(new AllowedTraitList(list.itemTrait));
				}
			}

			netIdentity.isDirty = true;

			// Put it in
			Inventory.ServerAdd(board, circuitBoardSlot);

			// Set initial state
			objectBehaviour.SetIsNotPushable(true);
			stateful.ServerChangeState(partsAddedState);
			putBoardInManually = false;
		}

		/// <summary>
		/// Transfers vendor content data to the first restock item found in partsInFrame
		/// </summary>
		private void TryTransferVendorContent()
		{
			foreach(var part in partsInFrame)
			{
				var restock = part.Key.GetComponent<VendingRestock>();
				if (restock != null)
				{
					restock.SetPreviousVendorContent(previousVendorContent);
					previousVendorContent = null;
					return;
				}
			}
		}

		private List<VendorItem> GetPreviousVendorContent()
		{
			foreach(var part in partsInFrame)
			{
				var restock = part.Key.GetComponent<VendingRestock>();
				if (restock != null && restock.PreviousVendorContent != null)
					return restock.PreviousVendorContent;
			}
			return previousVendorContent;
		}

		public void WhenDestroyed(DestructionInfo info)
		{
			if (CurrentState == partsAddedState || CurrentState == circuitAddedState)
			{
				RemoveCircuitAndParts();
			}

			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}

		private void RemoveCircuitAndParts()
		{
			Inventory.ServerDrop(circuitBoardSlot);
			stateful.ServerChangeState(wrenchedState);

			//If frame in mapped; count == 0 and its the only time putBoardInManually will be false as putting in board makes it true
			if (partsInFrame.Count != machineParts.machineParts.Length && !putBoardInManually)
			{
				foreach (var part in machineParts.machineParts)
				{
					//Spawn the part
					var partObj = Spawn.ServerPrefab(part.basicItem, gameObject.AssumedWorldPosServer(), gameObject.transform.parent, count: part.amountOfThisPart).GameObject;

					//Transfer vendor content if possible
					var restock = partObj.GetComponent<VendingRestock>();
					if (restock != null && previousVendorContent != null)
					{
						restock.SetPreviousVendorContent(previousVendorContent);
						previousVendorContent = null;
					}
				}
			}
			else
			{
				foreach (var item in partsInFrame)//Moves the hidden objects back on to the gameobject.
				{
					if (item.Key == null)//Shouldnt ever happen, but just incase
					{
						continue;
					}

					var pos = gameObject.AssumedWorldPosServer();

					item.Key.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(pos);
				}
			}

			putBoardInManually = false;
			spriteHandler.ChangeSprite((int) SpriteStates.BoxCable);

			//Reset data
			partsInFrame.Clear();
			basicPartsUsed.Clear();
		}

		private enum SpriteStates
		{
			Box = 0,
			BoxCable = 1,
			BoxCircuit = 2
		}
	}
}
