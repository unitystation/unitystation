using System;
using UnityEngine;

namespace Systems.CraftingV2
{
	[Serializable]
	public class RelatedRecipe
	{
		[SerializeField] private int ingredientIndex;
		[SerializeField] private CraftingRecipe recipe;

		public RelatedRecipe(CraftingRecipe recipe, int ingredientIndex)
		{
			this.recipe = recipe;
			this.ingredientIndex = ingredientIndex;
		}

		public CraftingRecipe Recipe => recipe;

		public int IngredientIndex => ingredientIndex;
	}
}