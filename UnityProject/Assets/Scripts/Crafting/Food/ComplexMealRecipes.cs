using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Cooking
{
	[CreateAssetMenu(fileName = "ComplexMealRecipe", menuName = "ScriptableObjects/Recipe/ComplexMealRecipe", order = 1)]
	[Serializable]
	public class ComplexMealRecipe : ScriptableObject
	{
		public class ComplexMealIngredients
		{
			public GameObject basicItem; //Basic item prefab used for when deconstructing mapped machines.

			public int amountOfThisIngredient = 1; // Amount of that ingredient
		}
		public GameObject meal;// Meal which will be spawned
		public GameObject mealBase;// MealBase needed to make this recipe
		public ComplexMealIngredients[] mealIngredients;
		public bool Check(List<ComplexMealIngredients> ingredients)
		{
			foreach(ComplexMealIngredients ingredient in mealIngredients)
			{
				if (!ingredients.Contains(ingredient)) return false;
			}
			return true;
		}
	}
}