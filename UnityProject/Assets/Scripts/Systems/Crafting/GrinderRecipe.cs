using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.Chemistry;

[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Recipe/GrinderRecipe")]
[Serializable]
public class GrinderRecipe : ScriptableObject
{
	public string Name;
	public Ingredient[] Ingredients;
	public int resultingAmount;
	public Reagent Output;

	public bool Check(List<Ingredient> other)
	{
		foreach (Ingredient ingredient in Ingredients)
		{
			if (!other.Contains(ingredient))
			{
				return false;
			}
		}
		return true;
	}
}
