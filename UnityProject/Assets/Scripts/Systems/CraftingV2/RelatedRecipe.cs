using System;
using UnityEngine;

[Serializable]
public class RelatedRecipe
{
	[SerializeField]
	private RecipeV2 recipe;

	public RecipeV2 Recipe => recipe;

	[SerializeField]
	private int ingredientIndex;

	public int IngredientIndex => ingredientIndex;

	public RelatedRecipe(RecipeV2 recipe, int ingredientIndex)
	{
		this.recipe = recipe;
		this.ingredientIndex = ingredientIndex;
	}
}
