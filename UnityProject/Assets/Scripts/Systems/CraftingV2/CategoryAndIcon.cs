using UnityEngine;

namespace Systems.CraftingV2
{
	[CreateAssetMenu(fileName = "CategoryAndIcon", menuName = "ScriptableObjects/Crafting/CategoryAndIcon")]
	public class CategoryAndIcon : ScriptableObject
	{
		[SerializeField]
		private RecipeCategory recipeCategory = RecipeCategory.Misc;

		[SerializeField]
		private Sprite categoryIcon;

		public RecipeCategory RecipeCategory => recipeCategory;

		public Sprite CategoryIcon => categoryIcon;
	}
}