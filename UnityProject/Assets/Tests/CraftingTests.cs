using System;
using System.Collections.Generic;
using System.Text;
using Systems.Cargo;
using Systems.CraftingV2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Util.PrefabUtils;

namespace Tests
{
	public class CraftingTests
	{
		private string recipesPath = "Assets/ScriptableObjects/Crafting/Recipes";

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

		[Test]
		public void CheckIngredientCrossLinks()
		{
			StringBuilder report = new StringBuilder();
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds =
				new Dictionary<GameObject, HashSet<GameObject>>();
			string[] prefabGuids = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});

			foreach (string prefabGuid in prefabGuids)
			{
				GameObject child = AssetDatabase.LoadAssetAtPath<GameObject>(
					AssetDatabase.GUIDToAssetPath(prefabGuid)
				);
				if (child.GetComponent<CraftingIngredient>() == null)
				{
					continue;
				}
				GameObject parent = PrefabExtensions.GetVariantBaseGameObject(child);
				if (parent == null)
				{
					continue;
				}

				if (parentsAndChilds.ContainsKey(parent) == false)
				{
					parentsAndChilds[parent] = new HashSet<GameObject>();
					continue;
				}

				parentsAndChilds[parent].Add(child);
			}

			string[] recipeGuids = AssetDatabase.FindAssets("t:ScriptableObject", new string[] {recipesPath});

			if (recipeGuids.Length == 0)
			{
				report.AppendLine("Recipe directory path was probably changed - can't find any recipe at path: " +
				                  $"{recipesPath}.");
			}

			foreach (string recipeGuid in recipeGuids)
			{
				var p = AssetDatabase.GUIDToAssetPath(recipeGuid);
				CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(
					p
				);
				for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
				{
					CheckIngredientsCrossLinks(
						recipe,
						i,
						recipe.RequiredIngredients[i].RequiredItem,
						parentsAndChilds,
						report
					);
				}
			}

			Logger.Log(report.ToString(), Category.Tests);
			Assert.IsEmpty(report.ToString());
		}

		private void CheckIngredientsCrossLinks(
			CraftingRecipe checkingRecipe,
			int indexInRecipe,
			GameObject requiredIngredient,
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds,
			StringBuilder report
		)
		{
			bool foundRecipe = false;
			foreach (
				RelatedRecipe relatedRecipe
				in requiredIngredient.GetComponent<CraftingIngredient>().RelatedRecipes
			)
			{
				if (relatedRecipe.Recipe != checkingRecipe)
				{
					continue;
				}

				foundRecipe = true;
				if (relatedRecipe.IngredientIndex != indexInRecipe)
				{
					report.AppendLine($"A crafting ingredient ({requiredIngredient}) has a wrong related recipe" +
					                  $"index. Expected: {indexInRecipe}, but found: {relatedRecipe.IngredientIndex}.");
				}
			}

			if (foundRecipe == false)
			{
				report.AppendLine($"A crafting ingredient ({requiredIngredient}) should have a link to a recipe " +
				                  $"({checkingRecipe}) in its RelatedRecipes list, since the recipe requires this " +
				                  "ingredient (prefab) or any of it's heirs (prefab variants).");
			}

			if (!parentsAndChilds.ContainsKey(requiredIngredient))
			{
				return;
			}
			foreach (GameObject child in parentsAndChilds[requiredIngredient])
			{
				CheckIngredientsCrossLinks(checkingRecipe, indexInRecipe, child, parentsAndChilds, report);
			}
		}
	}
}
