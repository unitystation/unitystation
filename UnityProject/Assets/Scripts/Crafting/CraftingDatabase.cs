using System;
using System.Collections.Generic;
using UnityEngine;


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
					return recipe.Output;
				}
			}
			return null;
		}

		public GameObject FindOutputMeal(string mealName)
		{
			foreach (Recipe recipe in recipeList)
			{
				if (recipe.Output.name == mealName)
				{
					return recipe.Output;
				}
			}
			return null;
		}
	}
