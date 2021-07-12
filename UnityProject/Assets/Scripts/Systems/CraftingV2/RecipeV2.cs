using System.Collections.Generic;
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

	/// <summary>
	/// Such recipes can be made simply by clicking one item on another, without calling the crafting menu.
	/// For example, roll out the dough with a rolling pin.
	/// In the crafting menu, these items will be at the bottom in the hidden list.
	/// </summary>
	/// <returns>True if no more than two items participate in the recipe, false otherwise.</returns>
	public bool IsSimple()
	{
		return RequiredIngredients.Count + RequiredToolTraits.Count == 2;
	}

	/// <summary>
	/// Checks for the presence of ingredients and tools necessary for the recipe.
	/// </summary>
	/// <param name="possibleIngredients">Ingredients that might be used for crafting.</param>
	/// <param name="possibleTools">Tools that might be used for crafting.</param>
	/// <returns>True if there are enough ingredients and tools for crafting, false otherwise.</returns>
	public bool CanBeCrafted(List<ItemAttributesV2> possibleIngredients, List<ItemAttributesV2> possibleTools)
	{
		return CheckPossibleIngredients(possibleIngredients) && CheckPossibleTools(possibleTools);
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
		foreach (IngredientV2 requiredIngredient in RequiredIngredients)
		{
			int countedAmount = 0;
			for (int i = 0; i < possibleIngredients.Count; i++)
			{
				if (requiredIngredient.RequiredItem.InitialName != possibleIngredients[i].InitialName)
				{
					continue;
				}

				if (++countedAmount == requiredIngredient.RequiredAmount)
				{
					break;
				}
			}

			if (countedAmount != requiredIngredient.RequiredAmount)
			{
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
		foreach (IngredientV2 requiredIngredient in requiredIngredients)
		{
			int usedIngredientsCounter = 0;
			foreach (ItemAttributesV2 possibleIngredient in possibleIngredients)
			{
				if (requiredIngredient.RequiredItem.InitialName != possibleIngredient.InitialName)
				{
					continue;
				}

				_ = Despawn.ServerSingle(possibleIngredient.gameObject);

				if (++usedIngredientsCounter >= requiredIngredient.RequiredAmount)
				{
					break;
				}
			}
		}

		CompleteCrafting(crafterGameObject);
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
