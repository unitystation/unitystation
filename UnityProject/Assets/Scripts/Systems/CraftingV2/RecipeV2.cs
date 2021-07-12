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
	/// <summary>
	/// Items that will be necessary and used for crafting. They will be deleted.
	/// </summary>
	[Tooltip("Items that will be necessary, used and deleted for crafting.")] [SerializeField]
	private List<IngredientV2> requiredIngredients = new List<IngredientV2>();

	public List<IngredientV2> RequiredIngredients => requiredIngredients;

	[Tooltip("What tools(item traits) should be present when creating a thing according to a recipe.")] [SerializeField]
	private List<ItemTrait> requiredToolTraits;

	public List<ItemTrait> RequiredToolTraits => requiredToolTraits;

	/// <summary>
	/// The resulting items after crafting.
	/// </summary>
	[Tooltip("The resulting items after crafting.")] [SerializeField]
	private List<GameObject> result;

	public List<GameObject> Result => result;

	[SerializeField] private CraftingCategory category = CraftingCategory.Misc;
	public CraftingCategory Category => category;

	[SerializeField]
	private List<RecipeV2> childrenRecipes = new List<RecipeV2>();

	public List<RecipeV2> ChildrenRecipes => childrenRecipes;

	[SerializeField]
	private string recipeName = "Undefined";

	public string RecipeName => recipeName;

	[SerializeField] [Min(0)]
	private float craftingTime;

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

	public bool CanBeCrafted(List<ItemAttributesV2> possibleIngredients, List<ItemAttributesV2> possibleTools)
	{
		return CheckPossibleIngredients(possibleIngredients) && CheckPossibleTools(possibleTools);
	}

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

	private void CompleteCrafting(GameObject crafterGameObject)
	{
		foreach (GameObject resultedGameObject in Result)
		{
			Spawn.ServerPrefab(resultedGameObject, crafterGameObject.WorldPosServer());
		}
	}
}
