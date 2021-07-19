using System.Collections.Generic;
using Items.Bureaucracy;

namespace Systems.CraftingV2
{
	public class RecipeBook : SimpleBook
	{
		private readonly List<CraftingRecipe> containsRecipes = new List<CraftingRecipe>();

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