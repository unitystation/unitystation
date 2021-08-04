#if UNITY_EDITOR
using UnityEditor;
#endif
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
		[SerializeField, ReadOnly]
		[Tooltip("Automated field - don't try to change it manually. " +
		         "Contains all the possible recipes that may be used for crafting.")]
		private List<CraftingRecipe> storedCraftingRecipes = new List<CraftingRecipe>();

		/// <summary>
		/// 	Contains all the possible recipes that may be used for crafting.
		/// </summary>
		public List<CraftingRecipe> StoredCraftingRecipes => storedCraftingRecipes;

#if UNITY_EDITOR
		public void RemoveRecipe(CraftingRecipe craftingRecipeToRemove)
		{
			int indexOfElementToDelete = StoredCraftingRecipes.IndexOf(craftingRecipeToRemove);
			StoredCraftingRecipes.RemoveAt(indexOfElementToDelete);
			EditorUtility.SetDirty(this);

			for (int i = indexOfElementToDelete; i < StoredCraftingRecipes.Count; i++)
			{
				StoredCraftingRecipes[i].IndexInSingleton--;
				EditorUtility.SetDirty(StoredCraftingRecipes[i]);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
#endif
	}
}