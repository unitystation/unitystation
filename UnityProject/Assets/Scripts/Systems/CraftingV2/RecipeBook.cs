using System.Collections.Generic;
using Items.Bureaucracy;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// 	The book that can teach players new recipes.
	/// </summary>
	public class RecipeBook : SimpleBook
	{
		[SerializeField, ReorderableList] [Tooltip("The recipes that a player will learn " +
		                                           "when the player have read the book.")]
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