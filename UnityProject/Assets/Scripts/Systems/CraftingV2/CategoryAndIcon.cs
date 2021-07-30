using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// 	A pair of values: a recipe category and a category icon.
	/// </summary>
	[CreateAssetMenu(fileName = "CategoryAndIcon", menuName = "ScriptableObjects/Crafting/CategoryAndIcon")]
	public class CategoryAndIcon : ScriptableObject
	{
		[SerializeField] [Tooltip("Which one recipe category does this object presents?")]
		private RecipeCategory recipeCategory = RecipeCategory.Misc;

		[SerializeField] [Tooltip("What icon does this object has?")]
		private Sprite categoryIcon;

		/// <summary>
		/// 	Which one recipe category does this object presents?
		/// </summary>
		public RecipeCategory RecipeCategory => recipeCategory;

		/// <summary>
		/// 	What icon does this object has?
		/// </summary>
		public Sprite CategoryIcon => categoryIcon;
	}
}