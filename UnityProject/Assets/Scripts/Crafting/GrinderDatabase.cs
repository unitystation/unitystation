using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GrinderDatabase
{
	public GrinderRecipe[] grinderRecipeList;
	public Chemistry.Reagent FindOutputReagent(string reagentName)
	{
		foreach (GrinderRecipe recipe in grinderRecipeList)
		{
			if (recipe.Output.Name == reagentName)
			{
				return recipe.Output;
			}
		}
		return null;
	}
	public Chemistry.Reagent FindReagentRecipe(List<Ingredient> ingredients)
	{
		foreach (GrinderRecipe recipe in grinderRecipeList)
		{
			if (recipe.Check(ingredients))
			{
				return recipe.Output;
			}
		}
		return null;
	}
	public int FindReagentAmount(List<Ingredient> ingredients)
	{
		foreach (GrinderRecipe recipe in grinderRecipeList)
		{
			if (recipe.Check(ingredients))
			{
				return recipe.resultingAmount;
			}
		}
		return 0;
	}
}