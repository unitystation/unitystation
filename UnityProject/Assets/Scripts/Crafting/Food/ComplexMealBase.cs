using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Machines;

namespace Cooking
{
	[System.Serializable]
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

	[System.Serializable]
	public class SyncListItem : SyncList<AllowedTraitList> { }

	/// <summary>
	/// Main Component for Machine Construction
	/// </summary>
	public class ComplexMealBase : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState ingredientsAddedState = null;
		[SerializeField] private StatefulState completeMeal = null;

		private ItemSlot circuitBoardSlot;//Index 0
		private IDictionary<ItemTrait, int> ingredientsUsed = new Dictionary<ItemTrait, int>();
		private IDictionary<GameObject, int> ingredientsInBase = new Dictionary<GameObject, int>();
		private Stateful stateful;

		private ComplexMealRecipe mealIngredients;

		private readonly SyncListItem allowedTraits = new SyncListItem();

		private List<AllowedTraitList> listOfAllowedTraits = new List<AllowedTraitList>();

		private ComplexMealRecipe.ComplexMealIngredients mealIngredientList;

		private bool putBoardInManually;

		private StatefulState CurrentState => stateful.CurrentState;
		private ObjectBehaviour objectBehaviour;

		private void Awake()
		{
			circuitBoardSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<ObjectBehaviour>();

			if (!isServer) return;

			if (CurrentState != ingredientsAddedState)
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
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			//different logic depending on state
			if (CurrentState == initialState)
			{
				//Add 5 cables or deconstruct
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Ingredient);
			}
			else if (CurrentState == ingredientsAddedState)
			{
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Ingredient);
			}

			return false;
		}

		/// <summary>
		/// What the server does if the interaction is valid on client
		/// </summary>
		/// <param name="interaction"></param>
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (CurrentState == ingredientsAddedState)
			{
				AddIngredientsInteraction(interaction);
			}
			else if (CurrentState == completeMeal)
			{
				MealCompleteInteraction(interaction);
			}
			else if (interaction.UsedObject == null)
			{
				foreach (var parts in mealIngredients.mealIngredients)
				{
					if (!ingredientsUsed.ContainsKey(parts.itemTrait)) return;

					if (ingredientsUsed[parts.itemTrait] != parts.amountOfThisIngredient)
					{
						return;
					}
				}
				stateful.ServerChangeState(completeMeal);
			}
		}

		private void AddIngredientsInteraction(HandApply interaction)
		{
			if (ItemTraitCheck(interaction))
			{
				var usedObject = interaction.UsedObject;

				Chat.AddActionMsgToChat(interaction, $"You place the {usedObject.ExpensiveName()} into the {GetComponent<ItemAttributesV2>().ArticleName}.",
					$"{interaction.Performer.ExpensiveName()} places the {usedObject.ExpensiveName()} into the {GetComponent<ItemAttributesV2>().ArticleName}.");

				//Process Part
				PartCheck(usedObject, interaction);
			}
		}

		/// <summary>
		/// Complete the meal, but only if the player
		/// </summary>
		/// <param name="interaction"></param>
		private void MealCompleteInteraction(HandApply interaction)
		{
			//Complete construction, spawn new machine and send data over to it.
			if (interaction.Intent == Intent.Help && interaction.UsedObject == null)
			{
				var spawnedObject = Spawn.ServerPrefab(mealIngredients.meal, SpawnDestination.At(gameObject)).GameObject.GetComponent<ComplexMeal>();

				if (spawnedObject == null)
				{
					Logger.LogWarning(mealIngredients.meal + " is missing the meal script!", Category.ItemSpawn);
					return;
				}

				//Send circuit board data to the new machine
				spawnedObject.SetIngredientsUsed(ingredientsUsed);
				spawnedObject.SetIngredientsInBase(ingredientsInBase);
				spawnedObject.SetIngredients(mealIngredients);

				//Despawn frame
				Despawn.ServerSingle(gameObject);
			}
			else if (interaction.Intent != Intent.Help && interaction.UsedObject == null)
			{
				stateful.ServerChangeState(initialState);
				if (ingredientsInBase.Count == 0)
				{
					foreach (var part in mealIngredients.mealIngredients)
					{
						Spawn.ServerPrefab(part.basicItem, gameObject.WorldPosServer(), gameObject.transform.parent, count: part.amountOfThisIngredient);
					}
				}
				else
				{
					foreach (var item in ingredientsInBase)//Moves the hidden objects back on to the gameobject.
					{
						if (item.Key == null)//Shouldnt ever happen, but just incase
						{
							continue;
						}

						var pos = gameObject.GetComponent<CustomNetTransform>().ServerPosition;

						item.Key.GetComponent<CustomNetTransform>().AppearAtPositionServer(pos);
					}
				}
			}
			else if (interaction.UsedObject != null)
			{
				stateful.ServerChangeState(ingredientsAddedState);
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
			for(int i = 0; i < mealIngredients.mealIngredients.Length; i++)
			{
				// If the interaction object has an itemtrait thats in the list, set the list machinePartsList variable as the list from the machineParts data from the circuit board.
				if (usedObject.GetComponent<ItemAttributesV2>().HasTrait(mealIngredients.mealIngredients[i].itemTrait))
				{
					machinePartsList = mealIngredients.mealIngredients[i];
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
				basicPartsUsed[itemTrait] = machinePartsList.amountOfThisPart;

				Inventory.ServerDrop(interaction.HandSlot);

				AddItemToDict(usedObject, needed, interaction);
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
				basicPartsUsed.Add(itemTrait, needed);

				Inventory.ServerDrop(interaction.HandSlot);

				AddItemToDict(usedObject, needed, interaction);

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

				newObject.GetComponent<CustomNetTransform>().DisappearFromWorldServer();

				if (newObject.transform.parent != gameObject.transform.parent)
				{
					newObject.transform.parent = gameObject.transform.parent;
				}

				partsInFrame.Add(newObject, amount);
			}
			// If not stackable send to hidden pos
			else
			{
				usedObject.GetComponent<CustomNetTransform>().DisappearFromWorldServer();

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
			foreach (var part in mealIngredients.mealIngredients)
			{
				if (Validations.HasUsedItemTrait(interaction, part.itemTrait) && (!basicPartsUsed.ContainsKey(part.itemTrait) || basicPartsUsed[part.itemTrait] != part.amountOfThisIngredient)) // Has items trait and we dont have enough yet
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
				msg = $"The {GetComponent<ItemAttributesV2>().ArticleName} is empty.";
			}

			if (CurrentState == ingredientsAddedState)
			{
				msg = $"The {GetComponent<ItemAttributesV2>().ArticleName} currently contains:\n";

				foreach (var parts in mealIngredients.mealIngredients)
				{
					msg += ingredientsUsed[parts.itemTrait];
					msg += " " + parts.itemTrait.name;

					if (ingredientsUsed[parts.itemTrait] > 1)
					{
						msg += "s";
					}

					msg += "\n";
				}
			}

			if (CurrentState == completeMeal)
			{
				msg = "Use a screwdriver to finish construction or use crowbar to remove circuit board.\n";
			}

			return msg;
		}

		/// <summary>
		/// Initializes this frame's state to be from a just-deconstructed machine
		/// </summary>
		/// <param name="machine"></param>
		public void ServerInitFromComputer(ComplexMeal machine)
		{
			spriteRender.sprite = boxCircuit;
			ServerChangeSprite(SpriteStates.BoxCircuit);

			// Create the circuit board
			var board = Spawn.ServerPrefab(machine.MachineBoardPrefab).GameObject;

			if (board == null)
			{
				Logger.LogWarning("MachineBoardPrefab was null", Category.ItemSpawn);
				return;
			}

			board.GetComponent<MachineCircuitBoard>().SetMachineParts(machine.MachineParts); // Basic item requirements to the circuit board

			board.GetComponent<ItemAttributesV2>().ServerSetArticleName(machine.MachineParts.NameOfCircuitBoard); // Sets name of board

			board.GetComponent<ItemAttributesV2>().ServerSetArticleDescription(machine.MachineParts.DescriptionOfCircuitBoard); // Sets desc of board

			// Basic items to the machine frame from the despawned machine
			machineParts = machine.MachineParts;

			allowedTraits.Clear();

			foreach (var list in machineParts.machineParts)
			{
				allowedTraits.Add(new AllowedTraitList(list.itemTrait));
			}

			partsInFrame = machine.PartsInFrame;

			basicPartsUsed = machine.BasicPartsUsed;

			// Put it in
			Inventory.ServerAdd(board, circuitBoardSlot);

			// Set initial state
			objectBehaviour.ServerSetPushable(false);
			stateful.ServerChangeState(partsAddedState);
			putBoardInManually = false;
		}

		private enum SpriteStates
		{
			Box = 0,
			BoxCable = 1,
			BoxCircuit = 2
		}
	}
}