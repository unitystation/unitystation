using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
	[Serializable]
	public class CraftingDatabase
	{
		public Recipe[] recipeList;

		public GameObject FindRecipe(List<Ingredient> ingredients)
		{
			foreach (Recipe recipe in recipeList)
			{
				if (recipe.Check(ingredients))
				{
					return recipe.output;
				}
			}
			return null;
		}

		public GameObject FindOutputMeal(string mealName)
		{
			foreach (Recipe recipe in recipeList)
			{
				if (recipe.output.name == mealName)
				{
					return recipe.output;
				}
			}
			return null;
		}
	}
}