using System;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// 	A pair of values: a link to a recipe that use an ingredient and ingredient's index in a recipe's required
	/// 	ingredients list.
	/// </summary>
	[Serializable]
	public class RelatedRecipe
	{
		[SerializeField, ReadOnly] [Tooltip("Automated field - don't try to change it manually. " +
		                                    "The ingredient's index in a recipe's required ingredients list.")]
		private int ingredientIndex;

		/// <summary>
		/// 	The ingredient's index in a recipe's required ingredients list.
		/// </summary>
		public int IngredientIndex => ingredientIndex;

		[SerializeField, ReadOnly] [Tooltip("Automated field - don't try to change it manually. " +
		                                    "The link to a recipe that may this ingredient.")]
		private CraftingRecipe recipe;

		/// <summary>
		/// 	The link to a recipe that may this ingredient.
		/// </summary>
		public CraftingRecipe Recipe => recipe;

		public RelatedRecipe(CraftingRecipe recipe, int ingredientIndex)
		{
			this.recipe = recipe;
			this.ingredientIndex = ingredientIndex;
		}
	}
}