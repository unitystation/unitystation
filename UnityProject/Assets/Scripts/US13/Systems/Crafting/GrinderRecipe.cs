using System;
using System.Collections.Generic;
using UnityEngine;

namespace US13.Systems.Crafting
{
	[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Recipe/GrinderRecipe")]
	[Serializable]
	public class GrinderRecipe : ScriptableObject
	{
		public string Name;
		public Ingredient[] Ingredients;
		public int resultingAmount;
		public Chemistry.Reagent Output;

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
}
