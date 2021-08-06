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

		public CraftingRecipe GetRecipeByIndex(int index)
		{
			return storedCraftingRecipes[index];
		}

		public int CountTotalStoredRecipes()
		{
			return storedCraftingRecipes.Count;
		}

#if UNITY_EDITOR
		public string GetStoredCraftingRecipesListFieldName()
		{
			return nameof(storedCraftingRecipes);
		}

		public void RemoveNulls()
		{
			for (int i = CountTotalStoredRecipes() - 1; i >= 0; i--)
			{
				if (storedCraftingRecipes[i] == null)
				{
					storedCraftingRecipes.RemoveAt(i);
				}
			}
		}

		public void FixRecipeIndexes()
		{
			for (int i = 0; i < storedCraftingRecipes.Count; i++)
			{
				storedCraftingRecipes[i].IndexInSingleton = i;
				EditorUtility.SetDirty(storedCraftingRecipes[i]);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public bool AddRecipeIfNecessary(CraftingRecipe recipe)
		{
			if (storedCraftingRecipes.Contains(recipe))
			{
				return false;
			}

			storedCraftingRecipes.Add(recipe);
			recipe.IndexInSingleton = storedCraftingRecipes.IndexOf(recipe);
			EditorUtility.SetDirty(Instance);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			return true;
		}

		public void RemoveRecipe(CraftingRecipe craftingRecipeToRemove)
		{
			int indexOfElementToDelete = storedCraftingRecipes.IndexOf(craftingRecipeToRemove);
			storedCraftingRecipes.RemoveAt(indexOfElementToDelete);
			EditorUtility.SetDirty(this);

			for (int i = indexOfElementToDelete; i < storedCraftingRecipes.Count; i++)
			{
				storedCraftingRecipes[i].IndexInSingleton--;
				EditorUtility.SetDirty(storedCraftingRecipes[i]);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
#endif
	}
}