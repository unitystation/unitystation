using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Recipe/GenericRecipe")]
[Serializable]
public class Recipe : ScriptableObject
{
	public string Name;
	public Ingredient[] Ingredients;
	public GameObject Output;

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
