using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class CraftingDatabase
{
	public Recipe[] recipeList;

	public Recipe FindRecipeFromIngredients(List<ItemAttributesV2> ingredients)
	{
		foreach (Recipe recipe in recipeList)
		{
			if (recipe.CanMakeRecipeWith(ingredients))
			{
				return recipe;
			}
		}
		return null;
	}

	public Recipe FindRecipeFromOutput(string mealName)
	{
		foreach (Recipe recipe in recipeList)
		{
			if (recipe.Output.name == mealName)
			{
				return recipe;
			}
		}
		return null;
	}
}