using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Systems.CraftingV2
{
	[CreateAssetMenu(
		fileName = "CraftingRecipeSingleton", menuName = "ScriptableObjects/Crafting/CraftingRecipeSingleton"
	)]
	public class CraftingRecipeSingleton : SingletonScriptableObject<CraftingRecipeSingleton>
	{
		[SerializeField, NaughtyAttributes.ReadOnly]
		private List<CraftingRecipe> storedCraftingRecipes = new List<CraftingRecipe>();

		public List<CraftingRecipe> StoredCraftingRecipes => storedCraftingRecipes;
	}
}