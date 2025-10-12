using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Items;
using Logs;
using UnityEngine;
using Mirror;
using ScriptableObjects;
using Machines;
using Objects.Machines;
using UI.Systems.Tooltips.HoverTooltips;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

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

		private List<PartReference> ObjectpartsInFrame = new List<PartReference>();

		public ItemStorage ItemStorage;

		private Stateful stateful;

		private MachineParts machineParts;

		private readonly SyncListItem allowedTraits = new SyncListItem();

		private List<AllowedTraitList> listOfAllowedTraits = new List<AllowedTraitList>();

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

			ItemStorage.ServerInventoryItemSlotSet += PartFrameTransfer;

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


			// Process initial SyncList payload
			for (int index = 0; index < allowedTraits.Count; index++)
				AllowedTraitsCallBack(SyncList<AllowedTraitList>.Operation.OP_ADD, index, new AllowedTraitList(), allowedTraits[index]);
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

						spriteHandler.SetCatalogueIndexSprite((int) SpriteStates.BoxCable);
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

				spriteHandler.SetCatalogueIndexSprite((int)SpriteStates.Box);
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

				spriteHandler.SetCatalogueIndexSprite((int)SpriteStates.BoxCircuit);
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
					int Required = parts.amountOfThisPart;

					Required -= NumberOfPartsForTrait(parts.itemTrait);

					if (Required > 0)
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
					Loggy.Warning(machineParts.machine + " is missing the machine script!", Category.Construction);
					return;
				}

				//Send circuit board data to the new machine
				spawnedObject.SetMachineParts(machineParts);
				spawnedObject.SetPartsInFrame(ItemStorage);


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

		public void PartFrameTransfer(Pickupable prevPart, Pickupable NewPart)
		{
			if (NewPart)
			{
				MachineParts.MachinePartList machinePartsList = null;
				// For all the list of data(itemtraits, amounts needed) in machine parts
				for(int i = 0; i < machineParts.machineParts.Length; i++)
				{
					// If the interaction object has an itemtrait thats in the list, set the list machinePartsList variable as the list from the machineParts data from the circuit board.
					if (NewPart.GetComponent<ItemAttributesV2>().HasTrait(machineParts.machineParts[i].itemTrait))
					{
						machinePartsList = machineParts.machineParts[i];
						break;

						// IF YOU WANT AN ITEM TO HAVE TWO ITEMTTRAITS WHICH CONTRIBUTE TO THE MACHINE BUILIDNG PROCESS, THIS NEEDS TO BE REFACTORED
						// all the stuff below needs to go into its own method which gets called here, replace the break;
					}
				}

				if (machinePartsList != null)
				{
					// Itemtrait currently being looked at.
					var itemTrait = machinePartsList.itemTrait;


					var StockTier = NewPart.GetComponent<StockTier>();

					int Tier = 1;

					if (StockTier != null)
					{
						Tier = StockTier.Tier;
					}

					ObjectpartsInFrame.Add(new PartReference()
					{
						itemObject = NewPart.gameObject,
						Slot = NewPart.ItemSlot,
						itemTrait = itemTrait,
						tier = Tier
					});
				}
			}
			else if (prevPart )
			{
				ObjectpartsInFrame.RemoveAll(x => x.itemObject == prevPart.gameObject);
			}
		}


		/// <summary>
		/// Function to process the part which has been applied to the frame
		/// </summary>
		/// <param name="usedObject"></param>
		/// <param name="interaction"></param>
		private void PartCheck(GameObject usedObject, HandApply interaction)
		{
			MachineParts.MachinePartList machinePartsList = null;
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


			if (usedObject.GetComponent<Stackable>() != null)
			{
				var Stacking = usedObject.GetComponent<Stackable>();


				var PreExisting = NumberOfPartsForTrait(itemTrait);

				needed -= PreExisting;

				var ExistingStacking=  ObjectpartsInFrame.FirstOrDefault(
					x => x.itemTrait == itemTrait
					     && x.itemObject.GetComponentCustom<Stackable>().StacksWith(Stacking));

				if (ExistingStacking?.itemTrait  != null)
				{
					var Amount = Mathf.Min(Stacking.Amount, needed);
					ExistingStacking.itemObject.GetComponent<Stackable>().ServerIncrease(Amount);
					Stacking.ServerConsume(Amount);
				}
				else
				{
					var objectToUse = Stacking.ServerTake(needed);
					var Slot = ItemStorage.GetBestSlotFor(objectToUse);

					var Currentslot = objectToUse.GetComponent<Pickupable>().ItemSlot;

					if (Currentslot != null)
					{
						Inventory.ServerTransfer(Currentslot,Slot);
					}
					else
					{
						Inventory.ServerAdd(objectToUse,Slot);
					}
				}
			}
			else
			{
				var Slot = ItemStorage.GetBestSlotFor(interaction.HandSlot.ItemObject);
				Inventory.ServerTransfer(interaction.HandSlot,Slot);
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
				if (Validations.HasItemTrait(interaction, part.itemTrait)
				    && (ObjectpartsInFrame.Any( x=> x.itemTrait == part.itemTrait) == false //Doesn't have any
				        || NumberOfPartsForTrait(part.itemTrait) < part.amountOfThisPart)) //  dont have enough yet
				{
					return true;
				}
			}

			return false;
		}

		public int NumberOfPartsForTrait(ItemTrait itemTrait)
		{
			return ObjectpartsInFrame.Where(x => x.itemTrait == itemTrait).Sum(x => x.itemObject.NumberOf());

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
				msg += RemainingItemsExamineText();
				msg += "Use crowbar to remove circuit board.\n";
			}

			if (CurrentState == partsAddedState)
			{
				msg = "Use a screwdriver to finish construction or use crowbar to remove circuit board.\n";
			}

			return msg;
		}

		private string RemainingItemsExamineText()
		{
			var msg = "You have these items left to add: \n";

			foreach (var parts in machineParts.machineParts)
			{
				var NumberSet = NumberOfPartsForTrait(parts.itemTrait);
				if (NumberSet == 0)//If false then we have none of the itemtrait
				{
					msg += parts.amountOfThisPart;
					msg += " " + parts.itemTrait.name;

					if (parts.amountOfThisPart > 1)
					{
						msg += "s";
					}

					msg += "\n";
				}
				else if (NumberSet != parts.amountOfThisPart)//If we have some but not enough of the itemtrait
				{
					msg += parts.amountOfThisPart - NumberSet;
					msg += " " + parts.itemTrait.name;

					if ((parts.amountOfThisPart - NumberSet) > 1)
					{
						msg += "s";
					}

					msg += "\n";
				}
			}
			return msg;
		}

		/// <summary>
		/// Initializes this frame's state to be from a just-deconstructed machine
		/// </summary>
		/// <param name="machine"></param>
		public void ServerInitFromComputer(Machine machine)
		{
			spriteHandler.SetCatalogueIndexSprite((int) SpriteStates.BoxCircuit);



			// Basic item requirements to the circuit board
			// Shouldn't be required that cool if you make a custom machine since the circuit board will magically become what you want

			// Basic items to the machine frame from the despawned machine
			machineParts = machine.MachineParts;
			ItemStorage.ServerTryTransferFrom(machine.PartsStorage);

			MachineCircuitBoard board = null;


			foreach (var slot in ItemStorage.GetItemSlots())
			{
				if (slot.Item.TryGetComponent(out board)) break;
			}

			board.GetComponent<MachineCircuitBoard>().SetMachineParts(machine.MachineParts);
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
				Loggy.Error($"Failed to find machine parts for {machineParts.OrNull()?.name ?? board.gameObject.ExpensiveName()}");
			}
			else
			{
				foreach (var list in machineParts.machineParts)
				{
					allowedTraits.Add(new AllowedTraitList(list.itemTrait));
				}
			}

			netIdentity.isDirty = true;

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
			foreach(var part in ObjectpartsInFrame)
			{
				var restock = part.itemObject.GetComponentCustom<VendingRestock>();
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
			foreach(var part in ObjectpartsInFrame)
			{
				var restock = part.itemObject.GetComponentCustom<VendingRestock>();
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

			ItemStorage.ServerDropAll();

			putBoardInManually = false;
			spriteHandler.SetCatalogueIndexSprite((int) SpriteStates.BoxCable);
			ObjectpartsInFrame.Clear();
		}

		private enum SpriteStates
		{
			Box = 0,
			BoxCable = 1,
			BoxCircuit = 2
		}
	}
}
