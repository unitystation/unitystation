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
	}
}
