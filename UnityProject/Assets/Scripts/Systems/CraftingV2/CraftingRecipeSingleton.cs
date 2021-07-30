using System.Collections.Generic;
using NaughtyAttributes;
using ScriptableObjects;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// 	Contains all possible recipes that may be used for crafting.
	/// </summary>
	[CreateAssetMenu(
		fileName = "CraftingRecipeSingleton", menuName = "ScriptableObjects/Crafting/CraftingRecipeSingleton"
	)]
	public class CraftingRecipeSingleton : SingletonScriptableObject<CraftingRecipeSingleton>
	{
		[SerializeField, ReadOnly] [Tooltip("Automated field - don't try to change it manually. " +
		                                    "Contains all the possible recipes that may be used for crafting.")]
		private List<CraftingRecipe> storedCraftingRecipes = new List<CraftingRecipe>();

		/// <summary>
		/// 	Contains all the possible recipes that may be used for crafting.
		/// </summary>
		public List<CraftingRecipe> StoredCraftingRecipes => storedCraftingRecipes;
	}
}