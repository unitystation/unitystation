using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
	[Serializable]
	public class Recipe
	{
		public Ingredient[] ingredients;
		public string name;
		public GameObject output;

		public bool Check(List<Ingredient> other)
		{
			foreach (Ingredient ingredient in ingredients)
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