using System.Collections.Generic;
using System.Text;
using Systems.Cargo;
using Systems.CraftingV2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests
{
	public class CraftingTests
	{
		[Test]
		public void CheckCraftingIndex()
		{
			var report = new StringBuilder();

			if (Utils.TryGetScriptableObjectGUID(typeof(CraftingRecipeSingleton), report, out string guid) == false)
			{
				Assert.Fail(report.ToString());
				return;
			}

			var prefabGUIDS = AssetDatabase.FindAssets("t:CraftingRecipe");
			foreach (var prefabGUID in prefabGUIDS)
			{
				var path = AssetDatabase.GUIDToAssetPath(prefabGUID);
				var toCheck = AssetDatabase.LoadMainAssetAtPath(path) as CraftingRecipe;
				if(toCheck == null) continue;

				if (toCheck.IndexInSingleton < 0)
				{
					report.AppendLine($"The recipe: {toCheck.name} index is -1. " +
					                  "Recipe is missing from the CraftingRecipeSingleton, use button on recipe to add it.");

					continue;
				}

				if (toCheck.IndexInSingleton > CraftingRecipeSingleton.Instance.CountTotalStoredRecipes() ||
				    CraftingRecipeSingleton.Instance.GetRecipeByIndex(toCheck.IndexInSingleton) != toCheck)
				{
					report.AppendLine($"The recipe: {toCheck.name} has incorrect index. " +
					                  "Perhaps this recipe has wrong indexInSingleton that doesn't match a real index in " +
					                  "the singleton. Regenerate the indexes in the CraftingRecipeSingleton to fix");
				}
			}

			Logger.Log(report.ToString(), Category.Tests);
			Assert.IsEmpty(report.ToString());
		}

		[Test]
		public void CheckCraftingListTest()
		{
			var report = new StringBuilder();

			var prefabGUIDS = AssetDatabase.FindAssets("t:CraftingRecipeList");
			foreach (var prefabGUID in prefabGUIDS)
			{
				var path = AssetDatabase.GUIDToAssetPath(prefabGUID);
				var toCheck = AssetDatabase.LoadMainAssetAtPath(path) as CraftingRecipeList;
				if(toCheck == null) continue;

				if (toCheck.CraftingRecipes.Contains(null))
				{
					report.AppendLine($"{toCheck.name} contains null recipe, please fix");
				}

				var checkHashset = new HashSet<CraftingRecipe>();

				foreach (var recipe in toCheck.CraftingRecipes)
				{
					if (checkHashset.Contains(recipe) == false)
					{
						checkHashset.Add(recipe);
					}
					else if (recipe != null)
					{
						report.AppendLine($"{toCheck.name} contains duplicated recipes, please fix");
						break;
					}
				}
			}

			Logger.Log(report.ToString(), Category.Tests);
			Assert.IsEmpty(report.ToString());
		}
	}
}
