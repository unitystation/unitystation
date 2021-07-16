using System;
using System.Collections.Generic;
using Systems.CraftingV2;
using Chemistry.Components;
using Items;
using Player;
using UnityEngine;

/// <summary>
/// A recipe for crafting. Turns a list of items-ingredients into a list of items-results(usually only one item).
/// </summary>
[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Crafting/Recipe")]
public class RecipeV2 : ScriptableObject
{
	[Tooltip("Items that will be necessary, used and deleted for crafting.")] [SerializeField]
	private List<IngredientV2> requiredIngredients = new List<IngredientV2>();

	/// <summary>
	/// Items that will be necessary and used for crafting. They will be deleted.
	/// </summary>
	public List<IngredientV2> RequiredIngredients => requiredIngredients;

	[SerializeField] private List<IngredientReagent> requiredReagents = new List<IngredientReagent>();

	public List<IngredientReagent> RequiredReagents => requiredReagents;

	[Tooltip("What tools(item traits) should be present when creating a thing according to a recipe.")] [SerializeField]
	private List<ItemTrait> requiredToolTraits;

	/// <summary>
	/// What tools(item traits) should be present when creating a thing according to a recipe.
	/// </summary>
	public List<ItemTrait> RequiredToolTraits => requiredToolTraits;

	[Tooltip("The resulting items after crafting.")] [SerializeField]
	private List<GameObject> result;

	/// <summary>
	/// The resulting items after crafting.
	/// </summary>
	public List<GameObject> Result => result;

	[SerializeField] private CraftingCategory category = CraftingCategory.Misc;

	/// <summary>
	/// Recipe's category. See PlayerCrafting.KnownRecipesByCategory
	/// </summary>
	public CraftingCategory Category => category;

	[SerializeField]
	[Tooltip("Similar recipes to this one. For example, a plasma spear is a subtype of a glass shard spear")]
	private List<RecipeV2> childrenRecipes = new List<RecipeV2>();

	/// <summary>
	/// Similar recipes to this one. For example, a plasma spear is a subtype of a glass shard spear.
	/// </summary>
	public List<RecipeV2> ChildrenRecipes => childrenRecipes;

	[SerializeField] [Tooltip("The name of the recipe.")]
	private string recipeName = "Undefined";

	/// <summary>
	/// The name of the recipe. The name of the result is not used, since there can be many results.
	/// </summary>
	public string RecipeName => recipeName;

	[SerializeField] [Min(0)] [Tooltip("The standard time that will be spent on crafting according to this recipe.")]
	private float craftingTime;

	/// <summary>
	/// The standard time that will be spent on crafting according to this recipe.
	/// </summary>
	public float CraftingTime => craftingTime;

	private bool isSimple = false;

	/// <summary>
	/// Such recipes can be made simply by clicking one item on another, without calling the crafting menu.
	/// For example, roll out the dough with a rolling pin.
	/// In the crafting menu, these items will be at the bottom in the hidden list.
	/// See Awake().
	/// </summary>
	private bool IsSimple => IsSimple;

	private void Awake()
	{
		isSimple = requiredIngredients.Count == 1;
	}

	/// <summary>
	/// Checks for the presence of ingredients and tools necessary for the recipe.
	/// </summary>
	/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
	/// <param name="possibleTools">Tools that might be used for crafting.</param>
	/// <returns>True if there are enough ingredients and tools for crafting, false otherwise.</returns>
	public bool CanBeCrafted(List<ItemAttributesV2> possibleIngredients, List<ItemAttributesV2> possibleTools)
	{
		return CheckPossibleIngredients(possibleIngredients)
		       && CheckPossibleTools(possibleTools)
		       && CheckPossibleReagents(possibleIngredients);
	}

	private bool CheckPossibleReagents(List<ItemAttributesV2> possibleReagentContainers)
	{
		foreach (IngredientReagent requiredReagent in requiredReagents)
		{
			float foundAmount = 0;
			foreach (ItemAttributesV2 possibleReagentContainer in possibleReagentContainers)
			{
				if (possibleReagentContainer.gameObject.TryGetComponent(out ReagentContainer reagentContainer))
				{
					foundAmount += reagentContainer.AmountOfReagent(requiredReagent.RequiredReagent);
				}
			}

			if (foundAmount < requiredReagent.RequiredAmount)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Checks for the presence of tools necessary for the recipe.
	/// </summary>
	/// <param name="possibleTools">Tools that might be used for crafting.</param>
	/// <returns>True if there are enough tools for crafting, false otherwise.</returns>
	private bool CheckPossibleTools(List<ItemAttributesV2> possibleTools)
	{
		foreach (ItemTrait itemTrait in requiredToolTraits)
		{
			bool foundRequiredToolTrait = false;
			foreach (ItemAttributesV2 possibleTool in possibleTools)
			{
				if (possibleTool.HasTrait(itemTrait))
				{
					foundRequiredToolTrait = true;
					break;
				}
			}

			if (!foundRequiredToolTrait)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Checks for the presence of ingredients necessary for the recipe.
	/// </summary>
	/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
	/// <returns>True if there are enough ingredients for crafting, false otherwise.</returns>
	private bool CheckPossibleIngredients(List<ItemAttributesV2> possibleIngredients)
	{
		for (int reqIngIndex = 0; reqIngIndex < RequiredIngredients.Count; reqIngIndex++)
		{
			int countedAmount = 0;
			foreach (ItemAttributesV2 possibleIngredient in possibleIngredients)
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
	/// Safe crafting method. Will craft the `Result` if all the requirements for the recipe were fulfilled.
	/// </summary>
	/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
	/// <param name="possibleTools">Tools that might be used for crafting.</param>
	/// <param name="crafterGameObject">The game object that crafting according to the recipe.</param>
	public void TryToCraft(
		List<ItemAttributesV2> possibleIngredients,
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
	/// Unsafe crafting method. Will craft the `Result` even if there are not enough necessary tools or ingredients.
	/// </summary>
	/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
	/// <param name="possibleTools">Tools that might be used for crafting.</param>
	/// <param name="crafterGameObject">The game object that crafting according to the recipe.</param>
	public void UnsafelyCraft(
		List<ItemAttributesV2> possibleIngredients,
		List<ItemAttributesV2> possibleTools,
		GameObject crafterGameObject
	)
	{
		UseReagents(possibleIngredients);
		UseIngredients(possibleIngredients);
		CompleteCrafting(crafterGameObject);
	}

	private void UseIngredients(List<ItemAttributesV2> possibleIngredients)
	{
		for (int reqIngIndex = 0; reqIngIndex < RequiredIngredients.Count; reqIngIndex++)
		{
			int usedIngredientsCounter = 0;
			foreach (ItemAttributesV2 possibleIngredient in possibleIngredients)
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
					break;
				}

				if (usedIngredientsCounter == RequiredIngredients[reqIngIndex].RequiredAmount)
				{
					break;
				}
			}
		}
	}

	private void UseReagents(List<ItemAttributesV2> possibleIngredients)
	{
		foreach (IngredientReagent requiredReagent in RequiredReagents)
		{
			float amountUsed = 0;
			foreach (ItemAttributesV2 possibleIngredient in possibleIngredients)
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

	private int UseIngredient(int reqIngIndex, ItemAttributesV2 possibleIngredient, int usedIngredientsCounter)
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
	/// Completes crafting the recipe, spawns the Result.
	/// </summary>
	/// <param name="crafterGameObject"></param>
	private void CompleteCrafting(GameObject crafterGameObject)
	{
		foreach (GameObject resultedGameObject in Result)
		{
			Spawn.ServerPrefab(resultedGameObject, crafterGameObject.WorldPosServer());
		}
	}
}