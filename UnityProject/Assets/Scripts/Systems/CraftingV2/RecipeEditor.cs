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
		private List<RecipeIngredient> LastSerializedIngredients = new List<RecipeIngredient>();

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

			foreach (RecipeIngredient requiredIngredient in ((RecipeV2) target).RequiredIngredients)
			{
				LastSerializedIngredients.Add((RecipeIngredient) requiredIngredient.Clone());
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
			foreach (RecipeIngredient requiredIngredient in recipe.RequiredIngredients)
			{
				LastSerializedIngredients.Add((RecipeIngredient) requiredIngredient.Clone());
			}
		}

		private void ClearRelatedRecipes()
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
					PrefabUtility.SavePrefabAsset(craftingIngredient.gameObject);
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
						out CraftingIngredient craftingIngredient
					)
				)
				{
					craftingIngredient.RelatedRecipes.Add(new RelatedRecipe(recipe, i));
					PrefabUtility.SavePrefabAsset(craftingIngredient.gameObject);
				}
			}
		}
	}
}