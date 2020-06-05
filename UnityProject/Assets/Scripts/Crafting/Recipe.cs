using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Recipe/GenericRecipe")]
[Serializable]
public class Recipe : ScriptableObject
{
	public string Name;
	public Ingredient[] Ingredients;
	public GameObject Output;
	public int OutputAmount = 1;

	public bool CanMakeRecipeWith(List<ItemAttributesV2> other)
	{
		foreach (Ingredient recipeIngredient in Ingredients)
		{
			int ingredientCount = 0;

			foreach (ItemAttributesV2 ingredientOffered in other)
			{
				if (ingredientOffered.ArticleName == recipeIngredient.ingredientName)
				{
					Stackable stck = ingredientOffered.GetComponent<Stackable>();
					if (stck != null)
					{
						ingredientCount += stck.Amount;
					}
					else
					{
						ingredientCount++;
					}
				}
			}

			if (ingredientCount < recipeIngredient.requiredAmount)
			{
				return false;
			}
		}
		return true;
	}

	public int AmountOfIngredientNeeded(ItemAttributesV2 ingredient)
	{
		foreach (Ingredient recipeIngredient in Ingredients)
		{
			if (ingredient.ArticleName == recipeIngredient.ingredientName)
			{
				return recipeIngredient.requiredAmount;
			}
		}

		return 0;
	}

	public bool Consume(List<ItemAttributesV2> ingredients, out List<ItemAttributesV2> remains)
	{
		remains = new List<ItemAttributesV2>(ingredients);
		if (!CanMakeRecipeWith(ingredients))
		{
			return false;
		}

		foreach (Ingredient recipeIngredient in Ingredients)
		{
			int amountToConsume = recipeIngredient.requiredAmount;

			List<ItemAttributesV2> current = new List<ItemAttributesV2>(remains);
			foreach (ItemAttributesV2 ingredientOffered in current)
			{
				if (ingredientOffered.ArticleName == recipeIngredient.ingredientName)
				{
					Stackable stck = ingredientOffered.GetComponent<Stackable>();
					if (stck != null)
					{
						int amt = Mathf.Min(stck.Amount, amountToConsume);
						if (amt == stck.Amount)
						{
							remains.Remove(ingredientOffered);
						}
						stck.ServerConsume(amt);
						amountToConsume -= amt;
					}
					else
					{
						Inventory.ServerDespawn(ingredientOffered.gameObject);
						remains.Remove(ingredientOffered);
						amountToConsume--;
					}

					if (amountToConsume <= 0)
					{
						break;
					}
				}
			}
		}
		return true;
	}
}
