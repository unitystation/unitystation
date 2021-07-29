using System.Collections.Generic;
using Items.Bureaucracy;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.CraftingV2
{
	public class RecipeBook : SimpleBook
	{
		[SerializeField, ReorderableList]
		private List<CraftingRecipe> containsRecipes = new List<CraftingRecipe>();

		protected override void FinishReading(ConnectedPlayer player)
		{
			base.FinishReading(player);
			LearnRecipes(player);
		}

		private void LearnRecipes(ConnectedPlayer player)
		{
			foreach (CraftingRecipe recipe in containsRecipes)
			{
				player.Script.PlayerCrafting.LearnRecipe(recipe);
			}
		}
	}
}