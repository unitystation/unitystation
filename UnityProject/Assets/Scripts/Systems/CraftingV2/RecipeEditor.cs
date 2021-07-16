using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Items;
using UnityEditor;

namespace Systems.CraftingV2
{
	[CustomEditor(typeof(RecipeV2))]
	public class RecipeEditor : Editor
	{
		private SerializedProperty spRecipeName;
		private SerializedProperty spCategory;
		private SerializedProperty spCraftingTime;
		private SerializedProperty spRequiredIngredients;
		private SerializedProperty spRequiredToolTraits;
		private SerializedProperty spRequiredReagents;
		private SerializedProperty spResult;
		private SerializedProperty spChildrenRecipes;

		private RecipeV2 recipe;
		private List<IngredientV2> LastSerializedIngredients = new List<IngredientV2>();

		private void OnEnable()
		{
			spRecipeName = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.RecipeName)));
			spCategory = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.Category)));
			spCraftingTime = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.CraftingTime)));
			spRequiredIngredients = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.RequiredIngredients)));
			spRequiredToolTraits = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.RequiredToolTraits)));
			spRequiredReagents = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.RequiredReagents)));
			spResult = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.Result)));
			spChildrenRecipes = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.ChildrenRecipes)));

			recipe = (RecipeV2) target;

			foreach (IngredientV2 requiredIngredient in ((RecipeV2) target).RequiredIngredients)
			{
				LastSerializedIngredients.Add((IngredientV2) requiredIngredient.Clone());
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(spRecipeName);
			EditorGUILayout.PropertyField(spCategory);
			EditorGUILayout.PropertyField(spCraftingTime);
			EditorGUILayout.PropertyField(spRequiredIngredients);
			EditorGUILayout.PropertyField(spRequiredToolTraits);
			EditorGUILayout.PropertyField(spRequiredReagents);
			EditorGUILayout.PropertyField(spResult);
			EditorGUILayout.PropertyField(spChildrenRecipes);

			if (EditorGUI.EndChangeCheck())
			{
				UpdateRelatedRecipes();
			}
		}

		// SampleText => sampleText
		private static string Title2Camel(string text)
		{
			return char.ToLowerInvariant(text[0]) + text.Substring(1);
		}

		private void UpdateRelatedRecipes()
		{
			serializedObject.ApplyModifiedProperties();
			bool updateIsUnnecessary = LastSerializedIngredients.Count == recipe.RequiredIngredients.Count;
			if (updateIsUnnecessary)
			{
				for (int i = 0; i < LastSerializedIngredients.Count; i++)
				{
					if (LastSerializedIngredients[i].RequiredItem != recipe.RequiredIngredients[i].RequiredItem)
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

			ClearRelatedRecipes();
			ReAddRelatedRecipes();
			UpdateLastSerializedIngredients();
		}

		private void UpdateLastSerializedIngredients()
		{
			LastSerializedIngredients.Clear();
			foreach (IngredientV2 requiredIngredient in recipe.RequiredIngredients)
			{
				LastSerializedIngredients.Add((IngredientV2) requiredIngredient.Clone());
			}
		}

		private void ClearRelatedRecipes()
		{
			foreach (IngredientV2 ingredient in recipe.RequiredIngredients)
			{
				if (ingredient.RequiredItem == null)
				{
					continue;
				}
				if (ingredient.RequiredItem.gameObject.TryGetComponent(out ItemAttributesV2 itemAttributes))
				{
					for (int i = itemAttributes.RelatedRecipes.Count - 1; i >= 0; i--)
					{
						if (itemAttributes.RelatedRecipes[i].Recipe == recipe)
						{
							itemAttributes.RelatedRecipes.RemoveAt(i);
						}
					}
					PrefabUtility.SavePrefabAsset(itemAttributes.gameObject);
				}
			}
		}

		private void ReAddRelatedRecipes()
		{
			for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
			{
				if (recipe.RequiredIngredients[i].RequiredItem == null)
				{
					continue;
				}
				if (
					recipe.RequiredIngredients[i].RequiredItem.gameObject.TryGetComponent(
						out ItemAttributesV2 itemAttributes
					)
				)
				{
					itemAttributes.RelatedRecipes.Add(new RelatedRecipe(recipe, i));
					PrefabUtility.SavePrefabAsset(itemAttributes.gameObject);
				}
			}
		}
	}
}