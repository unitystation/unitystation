using System;
using System.Collections.Generic;
using Systems.CraftingV2.ResultHandlers;
using Chemistry.Components;
using Items;
using NaughtyAttributes;
using Player;
using UnityEditor;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	///     A recipe for crafting. Turns a list of items-ingredients into a list of items-results(usually only one item).
	/// </summary>
	[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Crafting/Recipe")]
	public class CraftingRecipe : ScriptableObject
	{
		[SerializeField] private CraftingCategory category = CraftingCategory.Misc;

		[SerializeField]
		[Tooltip("Similar recipes to this one. For example, a plasma spear is a subtype of a glass shard spear")]
		private List<CraftingRecipe> childrenRecipes = new List<CraftingRecipe>();

		[SerializeField]
		[Min(0)]
		[Tooltip("The standard time that will be spent on crafting according to this recipe.")]
		private float craftingTime;

		[SerializeField] [Tooltip("The name of the recipe.")]
		private string recipeName = "Undefined";

		[Tooltip("Items that will be necessary, used and deleted for crafting.")] [SerializeField]
		private List<RecipeIngredient> requiredIngredients = new List<RecipeIngredient>();

		[SerializeField] private List<RecipeIngredientReagent> requiredReagents = new List<RecipeIngredientReagent>();

		[Tooltip("What tools(item traits) should be present when creating a thing according to a recipe.")]
		[SerializeField]
		private List<ItemTrait> requiredToolTraits;

		[Tooltip("The resulting items after crafting.")] [SerializeField]
		private List<GameObject> result;

		[SerializeField]
		private List<IResultHandler> resultHandlers = new List<IResultHandler>();

		/// <summary>
		///     Items that will be necessary and used for crafting. They will be deleted.
		/// </summary>
		public List<RecipeIngredient> RequiredIngredients => requiredIngredients;

		public List<RecipeIngredientReagent> RequiredReagents => requiredReagents;

		/// <summary>
		///     What tools(item traits) should be present when creating a thing according to a recipe.
		/// </summary>
		public List<ItemTrait> RequiredToolTraits => requiredToolTraits;

		/// <summary>
		///     The resulting items after crafting.
		/// </summary>
		public List<GameObject> Result => result;

		/// <summary>
		///     Recipe's category. See PlayerCrafting.KnownRecipesByCategory
		/// </summary>
		public CraftingCategory Category => category;

		/// <summary>
		///     Similar recipes to this one. For example, a plasma spear is a subtype of a glass shard spear.
		/// </summary>
		public List<CraftingRecipe> ChildrenRecipes => childrenRecipes;

		/// <summary>
		///     The name of the recipe. The name of the result is not used, since there can be many results.
		/// </summary>
		public string RecipeName => recipeName;

		/// <summary>
		///     The standard time that will be spent on crafting according to this recipe.
		/// </summary>
		public float CraftingTime => craftingTime;

		public List<IResultHandler> ResultHandlers => resultHandlers;

		/// <summary>
		///     Such recipes can be made simply by clicking one item on another, without calling the crafting menu.
		///     For example, roll out the dough with a rolling pin.
		///     In the crafting menu, these items will be at the bottom in the hidden list.
		/// </summary>
		public bool IsSimple
		{
			get => RequiredIngredients.Count + RequiredToolTraits.Count == 2;
		}

		/// <summary>
		///     Checks for the presence of ingredients and tools necessary for the recipe.
		/// </summary>
		/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
		/// <param name="possibleTools">Tools that might be used for crafting.</param>
		/// <returns>True if there are enough ingredients and tools for crafting, false otherwise.</returns>
		public bool CanBeCrafted(List<CraftingIngredient> possibleIngredients, List<ItemAttributesV2> possibleTools)
		{
			return CheckPossibleIngredients(possibleIngredients)
			       && CheckPossibleTools(possibleTools)
			       && CheckPossibleReagents(possibleIngredients);
		}

		private bool CheckPossibleReagents(List<CraftingIngredient> possibleReagentContainers)
		{
			foreach (RecipeIngredientReagent requiredReagent in requiredReagents)
			{
				float foundAmount = 0;
				foreach (CraftingIngredient possibleReagentContainer in possibleReagentContainers)
					if (possibleReagentContainer.gameObject.TryGetComponent(out ReagentContainer reagentContainer))
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

		/// <summary>
		///     Checks for the presence of tools necessary for the recipe.
		/// </summary>
		/// <param name="possibleTools">Tools that might be used for crafting.</param>
		/// <returns>True if there are enough tools for crafting, false otherwise.</returns>
		private bool CheckPossibleTools(List<ItemAttributesV2> possibleTools)
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

				if (!foundRequiredToolTrait)
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
		private bool CheckPossibleIngredients(List<CraftingIngredient> possibleIngredients)
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

						// okay, this is what we're looking for. We "use" this ingredient
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
		/// <param name="crafterGameObject">The game object that crafting according to the recipe.</param>
		public void TryToCraft(
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			GameObject crafterGameObject
		)
		{
			if (!CanBeCrafted(possibleIngredients, possibleTools))
			{
				return;
			}

			UnsafelyCraft(possibleIngredients, possibleTools, crafterGameObject);
		}

		/// <summary>
		///     Unsafe crafting method. Will craft the `Result` even if there are not enough necessary tools or ingredients.
		/// </summary>
		/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
		/// <param name="possibleTools">Tools that might be used for crafting.</param>
		/// <param name="crafterGameObject">The game object that crafting according to the recipe.</param>
		public void UnsafelyCraft(
			List<CraftingIngredient> possibleIngredients,
			List<ItemAttributesV2> possibleTools,
			GameObject crafterGameObject
		)
		{
			UseReagents(possibleIngredients);
			CompleteCrafting(crafterGameObject, UseIngredients(possibleIngredients));
		}

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

		private void UseReagents(List<CraftingIngredient> possibleIngredients)
		{
			foreach (RecipeIngredientReagent requiredReagent in RequiredReagents)
			{
				float amountUsed = 0;
				foreach (CraftingIngredient possibleIngredient in possibleIngredients)
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

		private int UseIngredient(int reqIngIndex, CraftingIngredient possibleIngredient, int usedIngredientsCounter)
		{
			if (possibleIngredient.TryGetComponent(out Stackable stackable))
			{
				if (
					usedIngredientsCounter + stackable.Amount
					<= RequiredIngredients[reqIngIndex].RequiredAmount
				)
				{
					stackable.ServerConsume(stackable.Amount);
				}
				else
				{
					stackable.ServerConsume(
						usedIngredientsCounter
						+ stackable.Amount
						- RequiredIngredients[reqIngIndex].RequiredAmount
					);
				}

				usedIngredientsCounter = Math.Min(
					usedIngredientsCounter + stackable.Amount,
					RequiredIngredients[reqIngIndex].RequiredAmount
				);
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
		/// <param name="crafterGameObject"></param>
		private void CompleteCrafting(GameObject crafterGameObject, List<CraftingIngredient> usedIngredients)
		{
			List<GameObject> spawnedResult = new List<GameObject>();
			foreach (GameObject resultedGameObject in Result)
			{
				spawnedResult.Add(
					Spawn.ServerPrefab(resultedGameObject, crafterGameObject.WorldPosServer()).GameObject
				);
			}

			foreach (IResultHandler resultHandler in ResultHandlers)
			{
				resultHandler.OnCraftingCompleted(spawnedResult, usedIngredients);
			}
		}
	}
}