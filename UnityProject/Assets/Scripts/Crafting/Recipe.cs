using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


[Serializable]
public class Recipe
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
