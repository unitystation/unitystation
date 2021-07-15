public class RelatedRecipe
{
	private RecipeV2 recipe;

	public RecipeV2 Recipe
	{
		get => recipe;
		set => recipe = value;
	}

	private int ingredientIndex;

	public int IngredientIndex
	{
		get => ingredientIndex;
		set => ingredientIndex = value;
	}

	public RelatedRecipe(RecipeV2 recipe, int ingredientIndex)
	{
		Recipe = recipe;
		IngredientIndex = ingredientIndex;
	}
}
