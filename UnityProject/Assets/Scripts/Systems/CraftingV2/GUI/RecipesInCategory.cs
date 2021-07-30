using System.Collections.Generic;

namespace Systems.CraftingV2.GUI
{
	/// <summary>
	/// 	A pair of values: a category button script and recipe buttons scripts according to the category.
	/// </summary>
	public class RecipesInCategory
	{
		private CategoryButtonScript categoryButtonScript;

		private List<RecipeButtonScript> recipeButtonScripts = new List<RecipeButtonScript>();

		public CategoryButtonScript CategoryButtonScript => categoryButtonScript;
		
		public List<RecipeButtonScript> RecipeButtonScripts => recipeButtonScripts;

		public RecipesInCategory(CategoryButtonScript categoryButtonScript)
		{
			this.categoryButtonScript = categoryButtonScript;
		}
	}
}