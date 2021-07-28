using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Systems.CraftingV2
{
	[CustomEditor(typeof(CraftingRecipe))]
	public class RecipeEditor : Editor
	{
		private readonly List<RecipeIngredient> lastSerializedIngredients = new List<RecipeIngredient>();

		private CraftingRecipe recipe;
		private SerializedProperty spCategory;
		private SerializedProperty spCraftingTime;
		private SerializedProperty spRecipeName;
		private SerializedProperty spRecipeIconOverride;
		private SerializedProperty spRequiredIngredients;
		private SerializedProperty spRequiredReagents;
		private SerializedProperty spRequiredToolTraits;
		private SerializedProperty spResult;
		private SerializedProperty spResultHandlers;

		private void OnEnable()
		{
			spRecipeName = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.RecipeName)));
			spRecipeIconOverride = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.RecipeIconOverride)));
			spCategory = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.Category)));
			spCraftingTime = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.CraftingTime)));
			spRequiredIngredients = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.RequiredIngredients)));
			spRequiredToolTraits = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.RequiredToolTraits)));
			spRequiredReagents = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.RequiredReagents)));
			spResult = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.Result)));
			spResultHandlers = serializedObject.FindProperty(Title2Camel(nameof(CraftingRecipe.ResultHandlers)));

			recipe = (CraftingRecipe) target;

			foreach (RecipeIngredient requiredIngredient in ((CraftingRecipe) target).RequiredIngredients)
			{
				lastSerializedIngredients.Add((RecipeIngredient) requiredIngredient.Clone());
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(spRecipeName);
			EditorGUILayout.PropertyField(spRecipeIconOverride);
			EditorGUILayout.PropertyField(spCategory);
			EditorGUILayout.PropertyField(spCraftingTime);
			EditorGUILayout.PropertyField(spRequiredIngredients);
			EditorGUILayout.PropertyField(spRequiredToolTraits);
			EditorGUILayout.PropertyField(spRequiredReagents);
			EditorGUILayout.PropertyField(spResult);
			EditorGUILayout.PropertyField(spResultHandlers);

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				UpdateRelatedRecipes();
				UpdateLastSerializedIngredients();
			}
		}

		// SampleText => sampleText
		private static string Title2Camel(string text)
		{
			return char.ToLowerInvariant(text[0]) + text.Substring(1);
		}

		private void UpdateRelatedRecipes()
		{
			bool updateIsUnnecessary = lastSerializedIngredients.Count == recipe.RequiredIngredients.Count;
			if (updateIsUnnecessary)
			{
				for (int i = 0; i < lastSerializedIngredients.Count; i++)
				{
					if (lastSerializedIngredients[i].RequiredItem != recipe.RequiredIngredients[i].RequiredItem)
					{
						updateIsUnnecessary = false;
						break;
					}
				}
			}

			if (updateIsUnnecessary)
			{
				return;
			}

			bool updateHasSimpleRelatedRecipe = recipe.IsSimple;

			ClearRelatedRecipes(updateHasSimpleRelatedRecipe);
			ReAddRelatedRecipes(updateHasSimpleRelatedRecipe);
		}

		private void UpdateLastSerializedIngredients()
		{
			lastSerializedIngredients.Clear();
			foreach (RecipeIngredient requiredIngredient in recipe.RequiredIngredients)
			{
				lastSerializedIngredients.Add((RecipeIngredient) requiredIngredient.Clone());
			}
		}

		private void ClearRelatedRecipes(bool updateHasSimpleRelatedRecipe)
		{
			foreach (RecipeIngredient recipeIngredient in recipe.RequiredIngredients)
			{
				if (recipeIngredient.RequiredItem == null)
				{
					continue;
				}
				if (recipeIngredient.RequiredItem.gameObject.TryGetComponent(out CraftingIngredient craftingIngredient))
				{
					for (int i = craftingIngredient.RelatedRecipes.Count - 1; i >= 0; i--)
					{
						if (craftingIngredient.RelatedRecipes[i].Recipe == recipe)
						{
							craftingIngredient.RelatedRecipes.RemoveAt(i);
						}
					}

					if (updateHasSimpleRelatedRecipe)
					{
						craftingIngredient.UpdateHasSimpleRelatedRecipe();
					}

					PrefabUtility.SavePrefabAsset(craftingIngredient.gameObject);
				}
			}
		}

		private void ReAddRelatedRecipes(bool updateHasSimpleRelatedRecipe)
		{
			for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
			{
				if (recipe.RequiredIngredients[i].RequiredItem == null)
				{
					continue;
				}
				if (
					recipe.RequiredIngredients[i].RequiredItem.gameObject.TryGetComponent(
						out CraftingIngredient craftingIngredient
					)
				)
				{
					craftingIngredient.RelatedRecipes.Add(new RelatedRecipe(recipe, i));
					if (updateHasSimpleRelatedRecipe)
					{
						craftingIngredient.UpdateHasSimpleRelatedRecipe();
					}
					PrefabUtility.SavePrefabAsset(craftingIngredient.gameObject);
				}
			}
		}
	}
}