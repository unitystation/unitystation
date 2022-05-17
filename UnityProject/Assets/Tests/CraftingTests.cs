using System;
using System.Collections.Generic;
using Systems.CraftingV2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Pool;
using Util;

namespace Tests
{
	public class CraftingTests
	{
		private readonly string recipesPath = "ScriptableObjects/Crafting/Recipes";

		[Test]
		public void CheckCraftingIndex()
		{
			const string singletonName = nameof(CraftingRecipeSingleton);

			var report = new TestReport();
			var recipeSingleton = Utils.GetSingleScriptableObject<CraftingRecipeSingleton>(report);
			foreach (var recipe in Utils.FindAssetsByType<CraftingRecipe>())
			{
				report.Clean()
					.FailIf(recipe.IndexInSingleton, Is.LessThan(0))
					.Append($"The recipe: {recipe.RecipeName} index is -1. ")
					.Append($"Recipe is missing from the {singletonName}, use button on recipe to add it.")
					.AppendLine()
					.MarkDirtyIfFailed()
					.FailIf(HasBadIndex(recipe))
					.Append($"The recipe: {recipe.RecipeName} has incorrect index. ")
					.Append("Perhaps this recipe has wrong indexInSingleton that doesn't match a real index in ")
					.Append($"the singleton. Regenerate the indexes in the {singletonName} to fix")
					.AppendLine();
			}

			report.Log().AssertPassed();

			bool HasBadIndex(CraftingRecipe recipe) =>
				recipe.IndexInSingleton > recipeSingleton.CountTotalStoredRecipes() ||
				recipeSingleton.GetRecipeByIndex(recipe.IndexInSingleton) != recipe;
		}

		[Test]
		public void CheckCraftingListTest()
		{
			var report = new TestReport();

			foreach (var recipeList in Utils.FindAssetsByType<CraftingRecipeList>())
			{
				report.FailIf(recipeList.CraftingRecipes.Contains(null))
					.AppendLine($"{recipeList.name} contains null recipe, please fix.");

				var checkHashset = new HashSet<CraftingRecipe>();

				foreach (var recipe in recipeList.CraftingRecipes)
				{
					if (checkHashset.Contains(recipe))
					{
						report.Fail().AppendLine($"{recipeList.name} contains duplicated recipes, please fix.");
						break;
					}

					checkHashset.Add(recipe);
				}
			}

			report.Log().AssertPassed();
		}

		/// <summary>
		/// 	Recipes have links to their ingredients, and the ingredients INCLUDING their heirs should have a link
		/// 	to the recipe. This test is supposed to find all ingredients that miss their link to the recipe
		/// 	or have a wrong recipe index.
		/// </summary>
		[Test]
		public void CheckIngredientCrossLinks()
		{
			var report = new TestReport();
			// thank unity we have no better way to see variants (heirs) of game objects.
			// This dictionary contains pairs of values:
			// <Parent, List<parent's heirs>> or something like that. Heirs can also have its heirs, so they also
			// can be parents and should be presented in this dictionary with the matching key.
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds =
				FindUtils.BuildAndGetInheritanceDictionaryOfPrefabs(new List<Type> {typeof(CraftingIngredient)});

			using var pool = ListPool<CraftingRecipe>.Get(out var recipes);
			recipes.AddRange(Utils.FindAssetsByType<CraftingRecipe>(recipesPath));
			report.FailIf(recipes.Count, Is.EqualTo(0))
				.AppendLine(
					$"Recipe directory path was probably changed - can't find any recipe at path: {recipesPath}.");

			foreach (var recipe in recipes)
			{
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

			report.Log().AssertPassed();
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
			TestReport report
		)
		{
			// has the ingredient a link to the recipe?
			bool foundRecipe = false;
			foreach (var relatedRecipe in requiredIngredient.GetComponent<CraftingIngredient>().RelatedRecipes)
			{
				if (relatedRecipe.Recipe != checkingRecipe) continue;

				foundRecipe = true;
				report.FailIfNot(relatedRecipe.IngredientIndex, Is.EqualTo(indexInRecipe))
					.Append($"A crafting ingredient ({requiredIngredient}) has a wrong related recipe ")
					.Append($"index. Expected: {indexInRecipe}, but found: {relatedRecipe.IngredientIndex}.")
					.Append("You could use Tools -> Crafting -> FixCraftingCrossLinks.")
					.AppendLine();
				break;
			}

			report.FailIfNot(foundRecipe)
				.Append($"A crafting ingredient ({requiredIngredient}) should have a link to a recipe ")
				.Append($"({checkingRecipe}) in its {nameof(RelatedRecipe)} list, since the recipe requires this ")
				.Append("ingredient (prefab), any of it's heirs (prefab variants) ")
				.Append("or even some parents (prefab sources aka bases).")
				.Append("You could use Tools -> Crafting -> FixCraftingCrossLinks.")
				.AppendLine();

			if (parentsAndChilds.ContainsKey(requiredIngredient) == false) return;

			foreach (GameObject child in parentsAndChilds[requiredIngredient])
			{
				CheckIngredientsCrossLinks(checkingRecipe, indexInRecipe, child, parentsAndChilds, report);
			}
		}
	}
}
