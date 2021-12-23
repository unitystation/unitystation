using System;
using System.Collections.Generic;
using System.Text;
using Systems.CraftingV2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Util;

namespace Tests
{
	public class CraftingTests
	{
		private readonly string recipesPath = "Assets/ScriptableObjects/Crafting/Recipes";

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

		/// <summary>
		/// 	Recipes have links to their ingredients, and the ingredients INCLUDING their heirs should have a link
		/// 	to the recipe. This test is supposed to find all ingredients that miss their link to the recipe
		/// 	or have a wrong recipe index.
		/// </summary>
		[Test]
		public void CheckIngredientCrossLinks()
		{
			StringBuilder report = new StringBuilder();
			// thank unity we have no better way to see variants (heirs) of game objects.
			// This dictionary contains pairs of values:
			// <Parent, List<parent's heirs>> or something like that. Heirs can also have its heirs, so they also
			// can be parents and should be presented in this dictionary with the matching key.
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds =
				FindUtils.BuildAndGetInheritanceDictionaryOfPrefabs(new List<Type> {typeof(CraftingIngredient)});

			// yes we can search without this recipesPath but i don't wanna make this test run for 5 minutes
			string[] recipeGuids = AssetDatabase.FindAssets("t:CraftingRecipe", new[] {recipesPath});

			if (recipeGuids.Length == 0)
			{
				report
					.AppendLine()
					.Append("Recipe directory path was probably changed - can't find any recipe at path: ")
					.Append($"{recipesPath}.");
			}

			foreach (string recipeGuid in recipeGuids)
			{
				CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(
					AssetDatabase.GUIDToAssetPath(recipeGuid)
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

		/// <summary>
		/// 	Check cross links of the ingredient-object and all its heirs recursively
		/// </summary>
		/// <param name="checkingRecipe">What recipe we need to check?</param>
		/// <param name="indexInRecipe">What index should be present in the ingredient?</param>
		/// <param name="requiredIngredient">What ingredient and its heirs we need to check?</param>
		/// <param name="parentsAndChilds">Dictionary of game objects and its heirs(variants)</param>
		/// <param name="report">StringBuilder that contains report of the unit test.</param>
		private void CheckIngredientsCrossLinks(
			CraftingRecipe checkingRecipe,
			int indexInRecipe,
			GameObject requiredIngredient,
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds,
			StringBuilder report
		)
		{
			// has the ingredient a link to the recipe?
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
					report
						.AppendLine()
						.Append($"A crafting ingredient ({requiredIngredient}) has a wrong related recipe ")
						.Append($"index. Expected: {indexInRecipe}, but found: {relatedRecipe.IngredientIndex}.")
						.Append("You could use Tools -> Crafting -> FixCraftingCrossLinks.");
				}
				break;
			}

			if (foundRecipe == false)
			{
				report
					.AppendLine()
					.Append($"A crafting ingredient ({requiredIngredient}) should have a link to a recipe ")
					.Append($"({checkingRecipe}) in its RelatedRecipes list, since the recipe requires this ")
					.Append("ingredient (prefab), any of it's heirs (prefab variants) ")
					.Append("or even some parents (prefab sources aka bases).")
					.Append("You could use Tools -> Crafting -> FixCraftingCrossLinks.");
			}

			if (parentsAndChilds.ContainsKey(requiredIngredient) == false)
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
