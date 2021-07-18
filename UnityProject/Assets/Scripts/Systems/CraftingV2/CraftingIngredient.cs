using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.CraftingV2
{
	public class CraftingIngredient : MonoBehaviour
	{
		[SerializeField] [ReadOnly] [Tooltip("Recipes that have this item as an ingredient.")]
		private List<RelatedRecipe> relatedRecipes = new List<RelatedRecipe>();

		/// <summary>
		///     Recipes that have this item as an ingredient.
		/// </summary>
		public List<RelatedRecipe> RelatedRecipes => relatedRecipes;
	}
}