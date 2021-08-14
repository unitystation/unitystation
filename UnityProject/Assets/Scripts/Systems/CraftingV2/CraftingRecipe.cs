using System;
using System.Collections.Generic;
using Systems.CraftingV2.ResultHandlers;
using Chemistry.Components;
using Items;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	///     A recipe for crafting. Turns a list of items-ingredients into a list of items-results(usually only one item).
	/// </summary>
	[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "ScriptableObjects/Crafting/CraftingRecipe")]
	public class CraftingRecipe : ScriptableObject
	{
		[SerializeField] [Tooltip("The recipe category.")]
		private RecipeCategory category = RecipeCategory.Misc;

		/// <summary>
		///     Recipe's category. See PlayerCrafting.KnownRecipesByCategory
		/// </summary>
		public RecipeCategory Category => category;

		[SerializeField]
		[Min(0)]
		[Tooltip("The standard time that will be spent on crafting according to this recipe.")]
		private float craftingTime;

		/// <summary>
		///     The standard time that will be spent on crafting according to this recipe.
		/// </summary>
		public float CraftingTime => craftingTime;

		[SerializeField] [Tooltip("The name of the recipe.")]
		private string recipeName = "Undefined";

		/// <summary>
		///     The name of the recipe. The name of the result is not used, since there can be many results.
		/// </summary>
		public string RecipeName => recipeName;


		[SerializeField]
		[Tooltip("The icon(sprite) that will be used for a recipe button. If it's empty(null), " +
		         "then the recipe button will use a first result's sprite found.")]
		private Sprite recipeIconOverride;

		/// <summary>
		/// The icon(sprite) that will be used for a recipe button. If it's empty(null),
		/// then the recipe button will use a first result's sprite found.
		/// </summary>
		public Sprite RecipeIconOverride => recipeIconOverride;


		[SerializeField] [Tooltip("The recipe description.")]
		private string recipeDescription = "";

		/// <summary>
		/// 	The recipe description.
		/// </summary>
		public string RecipeDescription => recipeDescription;

		[Tooltip("Items that will be necessary, used and deleted for crafting.")] [SerializeField]
		private List<RecipeIngredient> requiredIngredients = new List<RecipeIngredient>();

		/// <summary>
		///     Items that will be necessary and used for crafting. They will be deleted.
		/// </summary>
		public List<RecipeIngredient> RequiredIngredients => requiredIngredients;

		[SerializeField] [Tooltip("The reagents that are necessary for crafting according to the recipe.")]
		private List<RecipeIngredientReagent> requiredReagents = new List<RecipeIngredientReagent>();

		/// <summary>
		/// 	The reagents that are necessary for crafting according to the recipe.
		/// </summary>
		public List<RecipeIngredientReagent> RequiredReagents => requiredReagents;

		[Tooltip("What tools(item traits) should be present when crafting according to the recipe.")] [SerializeField]
		private List<ItemTrait> requiredToolTraits;

		/// <summary>
		///     What tools(item traits) should be present when creating a thing according to a recipe.
		/// </summary>
		public List<ItemTrait> RequiredToolTraits => requiredToolTraits;

		[SerializeField] [Tooltip("The resulting game objects after crafting.")]
		private List<GameObject> result;

		/// <summary>
		///     The resulting items after crafting.
		/// </summary>
		public List<GameObject> Result => result;

		[SerializeField]
		[Tooltip("The special handlers that handle specified craft actions. For example, " +
		         "we can set a spear's damage according to a glass shard used for crafting.")]
		private List<IResultHandler> resultHandlers = new List<IResultHandler>();

		/// <summary>
		/// 	The special handlers that handle specified craft actions. For example,
		/// 	we can set a spear's damage according to a glass shard used for crafting.
		/// </summary>
		public List<IResultHandler> ResultHandlers => resultHandlers;

		[SerializeField, ReadOnly]
		[Tooltip("Automated field - don't try to change it manually. " +
		         "Such recipes can be made simply by clicking one item on another, " +
		         "without calling the crafting menu. " +
		         "For example, roll out the dough with a rolling pin.")]
		private bool isSimple;

		/// <summary>
		///     Such recipes can be made simply by clicking one item on another, without calling the crafting menu.
		///     For example, roll out the dough with a rolling pin.
		/// </summary>
		public bool IsSimple
		{
			get => isSimple;
#if UNITY_EDITOR
			set { isSimple = value; }
#endif
		}

		[SerializeField, ReadOnly]
		[Tooltip("Automated field - don't try to change it manually. " +
		         "The position(index) in the CraftingRecipeSingleton.")]
		private int indexInSingleton = -1;

		/// <summary>
		/// 	The position(index) in the CraftingRecipeSingleton.
		/// </summary>
		public int IndexInSingleton
		{
			get => indexInSingleton;
#if UNITY_EDITOR
			set { indexInSingleton = value; }
#endif
		}

		/// <summary>
		///     Checks for the presence of ingredients, reagents and tools necessary for the recipe.
		/// </summary>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or reagent containers) that might be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that might be used for crafting.</param>
		/// <param name="reagentContainers">
		/// 	The possible reagent containers that might be used for crafting.
		/// </param>
		/// <returns>True if there are enough ingredients and tools for crafting, false otherwise.</returns>
		public CraftingStatus CanBeCrafted(
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<ReagentContainer> reagentContainers
		)
		{
			if (CheckPossibleIngredients(possibleIngredients) == false)
			{
				return CraftingStatus.NotEnoughIngredients;
			}

			if (CheckPossibleTools(possibleTools) == false)
			{
				return CraftingStatus.NotEnoughTools;
			}

			if (CheckPossibleReagents(reagentContainers) == false)
			{
				return CraftingStatus.NotEnoughReagents;
			}

			return CraftingStatus.AllGood;
		}

		/// <summary>
		///     Checks for the presence of ingredients, reagents and tools necessary for the recipe.
		/// </summary>
		/// <param name="possibleIngredients">
		/// 	The ingredients(or reagent containers) that might be used for crafting.
		/// </param>
		/// <param name="possibleTools">The tools that might be used for crafting.</param>
		/// <param name="reagents">
		/// 	The possible reagents(a pair of values: a reagent's index in the singleton and its amount)
		/// 	that might be used for crafting.
		/// </param>
		/// <returns>True if there are enough ingredients and tools for crafting, false otherwise.</returns>
		public CraftingStatus CanBeCrafted(
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<KeyValuePair<int, float>> reagents
		)
		{
			if (CheckPossibleIngredients(possibleIngredients) == false)
			{
				return CraftingStatus.NotEnoughIngredients;
			}

			if (CheckPossibleTools(possibleTools) == false)
			{
				return CraftingStatus.NotEnoughTools;
			}

			if (CheckPossibleReagents(reagents) == false)
			{
				return CraftingStatus.NotEnoughReagents;
			}

			return CraftingStatus.AllGood;
		}

		public CraftingStatus CanBeCraftedIgnoringReagents(
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools
		)
		{
			if (CheckPossibleIngredients(possibleIngredients) == false)
			{
				return CraftingStatus.NotEnoughIngredients;
			}

			if (CheckPossibleTools(possibleTools) == false)
			{
				return CraftingStatus.NotEnoughTools;
			}

			return CraftingStatus.AllGood;
		}

		/// <summary>
		///     Checks for the presence of reagents necessary for the recipe.
		/// </summary>
		/// <param name="reagentContainers">The reagent containers that might be used for crafting.</param>
		/// <returns>True if there are enough reagents for crafting, false otherwise.</returns>
		public bool CheckPossibleReagents(List<ReagentContainer> reagentContainers)
		{
			foreach (RecipeIngredientReagent requiredReagent in requiredReagents)
			{
				float foundAmount = 0;
				foreach (ReagentContainer reagentContainer in reagentContainers)
				{
					foundAmount += reagentContainer.AmountOfReagent(requiredReagent.RequiredReagent);
				}

				if (foundAmount < requiredReagent.RequiredAmount)
				{
					return false;
				}
			}

			return true;
		}

		public bool CheckPossibleReagents(List<KeyValuePair<int, float>> possibleReagents)
		{
			foreach (RecipeIngredientReagent requiredIngredientReagent in requiredReagents)
			{
				float foundAmount = 0;
				foreach (KeyValuePair<int, float> possibleReagent in possibleReagents)
				{
					if (possibleReagent.Key != requiredIngredientReagent.RequiredReagent.IndexInSingleton)
					{
						continue;
					}

					foundAmount += possibleReagent.Value;

					if (foundAmount >= requiredIngredientReagent.RequiredAmount)
					{
						break;
					}
				}

				if (foundAmount < requiredIngredientReagent.RequiredAmount)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		///     Checks for the presence of tools necessary for the recipe.
		/// </summary>
		/// <param name="possibleTools">Tools that might be used for crafting.</param>
		/// <returns>True if there are enough tools for crafting, false otherwise.</returns>
		public bool CheckPossibleTools(List<ItemAttributesV2> possibleTools)
		{
			foreach (ItemTrait itemTrait in requiredToolTraits)
			{
				bool foundRequiredToolTrait = false;
				foreach (ItemAttributesV2 possibleTool in possibleTools)
					if (possibleTool.HasTrait(itemTrait))
					{
						foundRequiredToolTrait = true;
						break;
					}

				if (foundRequiredToolTrait == false)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		///     Checks for the presence of ingredients necessary for the recipe.
		/// </summary>
		/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
		/// <returns>True if there are enough ingredients for crafting, false otherwise.</returns>
		public bool CheckPossibleIngredients(List<CraftingIngredient> possibleIngredients)
		{
			for (int reqIngIndex = 0; reqIngIndex < RequiredIngredients.Count; reqIngIndex++)
			{
				int countedAmount = 0;
				foreach (CraftingIngredient possibleIngredient in possibleIngredients)
				{
					foreach (RelatedRecipe relatedRecipe in possibleIngredient.RelatedRecipes)
					{
						// is it not an ingredient in this recipe?
						if (relatedRecipe.Recipe != this)
						{
							continue;
						}

						// is the ingredient included in this recipe, but we are still processing another ingredient?
						if (reqIngIndex != relatedRecipe.IngredientIndex)
						{
							continue;
						}

						// ok, this is what we're looking for. We "use" this ingredient
						if (possibleIngredient.TryGetComponent(out Stackable stackable))
						{
							countedAmount = Math.Min(
								countedAmount + stackable.Amount,
								RequiredIngredients[reqIngIndex].RequiredAmount
							);
						}

						else
						{
							countedAmount++;
						}

						break;
					}

					// do we have enough ingredients of this type when "using" the possibleIngredient?
					if (countedAmount == RequiredIngredients[reqIngIndex].RequiredAmount)
					{
						// yes, so let's search for another requiredIngredient
						break;
					}
				}

				// did we looked through all the possibleIngredients, but did not find enough necessary ones?
				if (countedAmount != RequiredIngredients[reqIngIndex].RequiredAmount)
				{
					// yes, so crafting according to the recipe is impossible
					return false;
				}
			}

			return true;
		}

		/// <summary>
		///     Safe crafting method. Will craft the `Result` if all the requirements for the recipe were fulfilled.
		/// </summary>
		/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
		/// <param name="possibleTools">Tools that might be used for crafting.</param>
		/// <param name="reagentContainers">Reagent containers that might be used for crafting.</param>
		/// <param name="crafterPlayerScript">The player that crafting according to the recipe.</param>
		public void TryToCraft(
			PlayerScript crafterPlayerScript,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<ReagentContainer> reagentContainers
		)
		{
			if (CanBeCrafted(possibleIngredients, possibleTools, reagentContainers) != CraftingStatus.AllGood)
			{
				return;
			}

			UnsafelyCraft(crafterPlayerScript, possibleIngredients, possibleTools, reagentContainers);
		}

		/// <summary>
		///     Unsafe crafting method. Will craft the `Result` even if there are not enough necessary tools or ingredients.
		/// </summary>
		/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
		/// <param name="possibleTools">Tools that might be used for crafting.</param>
		/// <param name="reagentContainers">Reagents that might be used for crafting.</param>
		/// <param name="crafterPlayerScript">The player that crafting according to the recipe.</param>
		public void UnsafelyCraft(
			PlayerScript crafterPlayerScript,
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			List<ReagentContainer> reagentContainers
		)
		{
			UseReagents(possibleIngredients);
			CompleteCrafting(crafterPlayerScript, UseIngredients(possibleIngredients));
		}

		/// <summary>
		/// 	Uses(despawns) the ingredients necessary for crafting according to the recipe.
		/// </summary>
		/// <param name="possibleIngredients">The ingredients that might be used for crafting.</param>
		/// <returns>Used ingredients.</returns>
		private List<CraftingIngredient> UseIngredients(List<CraftingIngredient> possibleIngredients)
		{
			List<CraftingIngredient> usedIngredients = new List<CraftingIngredient>();
			for (int reqIngIndex = 0; reqIngIndex < RequiredIngredients.Count; reqIngIndex++)
			{
				int usedIngredientsCounter = 0;
				foreach (CraftingIngredient possibleIngredient in possibleIngredients)
				{
					foreach (RelatedRecipe relatedRecipe in possibleIngredient.RelatedRecipes)
					{
						// is it not an ingredient in this recipe?
						if (relatedRecipe.Recipe != this)
						{
							continue;
						}

						// is the ingredient included in this recipe, but we are still processing another ingredient?
						if (reqIngIndex != relatedRecipe.IngredientIndex)
						{
							continue;
						}

						// okay, this is what we're looking for. We use this ingredient
						usedIngredientsCounter = UseIngredient(reqIngIndex, possibleIngredient, usedIngredientsCounter);
						usedIngredients.Add(possibleIngredient);
						break;
					}

					if (usedIngredientsCounter == RequiredIngredients[reqIngIndex].RequiredAmount)
					{
						break;
					}
				}
			}

			return usedIngredients;
		}

		/// <summary>
		/// 	Uses(removes) reagents necessary for the crafting recipe.
		/// </summary>
		/// <param name="possibleReagentContainers">
		/// 	The possible reagent containers whose content(reagents) might be used for crafting.
		/// </param>
		private void UseReagents(List<CraftingIngredient> possibleReagentContainers)
		{
			foreach (RecipeIngredientReagent requiredReagent in RequiredReagents)
			{
				float amountUsed = 0;
				foreach (CraftingIngredient possibleIngredient in possibleReagentContainers)
				{
					if (possibleIngredient.gameObject.TryGetComponent(out ReagentContainer reagentContainer))
					{
						amountUsed += reagentContainer.Subtract(
							requiredReagent.RequiredReagent, requiredReagent.RequiredAmount - amountUsed
						);
					}

					if (amountUsed >= requiredReagent.RequiredAmount)
					{
						break;
					}
				}
			}
		}

		/// <summary>
		/// 	Uses(removes) the specified ingredient.
		/// </summary>
		/// <param name="reqIngIndex">
		/// 	An index that points to the required ingredient in the RequiredIngredients list.
		/// </param>
		/// <param name="possibleIngredient">
		/// 	The possible ingredient associated with the RequiredIngredients[reqIngIndex]
		/// </param>
		/// <param name="usedIngredientsCounter">How many ingredients have already been found?</param>
		/// <returns>
		/// 	The total amount of ingredients used to fulfil the RequiredIngredients[reqIngIndex] requirement.
		/// </returns>
		private int UseIngredient(int reqIngIndex, CraftingIngredient possibleIngredient, int usedIngredientsCounter)
		{
			if (possibleIngredient.TryGetComponent(out Stackable stackable))
			{
				int consumed = usedIngredientsCounter + stackable.Amount
				               <= RequiredIngredients[reqIngIndex].RequiredAmount
				               ? stackable.Amount
				               : RequiredIngredients[reqIngIndex].RequiredAmount - usedIngredientsCounter;
				stackable.ServerConsume(consumed);
				usedIngredientsCounter += consumed;
			}
			else
			{
				usedIngredientsCounter++;
				_ = Despawn.ServerSingle(possibleIngredient.gameObject);
			}

			return usedIngredientsCounter;
		}

		/// <summary>
		///     Completes crafting the recipe, spawns the Result.
		/// </summary>
		/// <param name="crafterPlayerScript">The player that crafted according to the recipe.</param>
		/// <param name="usedIngredients">
		/// 	The ingredients that were used to fulfil the requirements for the recipe.
		/// </param>
		private void CompleteCrafting(PlayerScript crafterPlayerScript, List<CraftingIngredient> usedIngredients)
		{
			List<GameObject> spawnedResult = new List<GameObject>();
			foreach (GameObject resultedGameObject in Result)
			{
				spawnedResult.Add(
					Spawn.ServerPrefab(
						resultedGameObject,
						crafterPlayerScript.PlayerSync.ClientPosition
					).GameObject
				);
			}

			foreach (IResultHandler resultHandler in ResultHandlers)
			{
				resultHandler.OnCraftingCompleted(spawnedResult, usedIngredients);
			}

			if (spawnedResult.Count == 1)
			{
				Inventory.ServerAdd(spawnedResult[0], crafterPlayerScript.DynamicItemStorage.GetBestHand());
			}
		}
	}
}