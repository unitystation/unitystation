using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
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
		private SerializedProperty spResult;
		private SerializedProperty spChildrenRecipes;

		private List<SerializedProperty> LastSerializedIngredients;

		private void OnEnable()
		{
			spRecipeName = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.RecipeName)));
			spCategory = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.Category)));
			spCraftingTime = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.CraftingTime)));
			spRequiredIngredients = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.RequiredIngredients)));
			spRequiredToolTraits = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.RequiredToolTraits)));
			spResult = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.Result)));
			spChildrenRecipes = serializedObject.FindProperty(Title2Camel(nameof(RecipeV2.ChildrenRecipes)));

			LastSerializedIngredients = new List<SerializedProperty>();
			for (int i = 0; i < spRequiredIngredients.arraySize; i++)
			{
				LastSerializedIngredients.Add(spRequiredIngredients.GetArrayElementAtIndex(i));
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
			EditorGUILayout.PropertyField(spResult);
			EditorGUILayout.PropertyField(spChildrenRecipes);

			if (EditorGUI.EndChangeCheck())
			{
				bool updateRelatedRecipes = false;
				if (LastSerializedIngredients.Count == spRequiredIngredients.arraySize)
				{
					for (int i = 0; i < spRequiredIngredients.arraySize; i++)
					{
						if (!LastSerializedIngredients[i].Equals(spRequiredIngredients.GetArrayElementAtIndex(i)))
						{
							updateRelatedRecipes = true;
							break;
						}
					}
				}
				else
				{
					updateRelatedRecipes = true;
				}

				if (updateRelatedRecipes)
				{
					ClearRelatedRecipes((RecipeV2) target);
					serializedObject.ApplyModifiedProperties();
					ReAddRelatedRecipes((RecipeV2) target);
					return;
				}
				serializedObject.ApplyModifiedProperties();
			}
		}

		// SampleText => sampleText
		private static string Title2Camel(string text)
		{
			return char.ToLowerInvariant(text[0]) + text.Substring(1);
		}

		private static void ClearRelatedRecipes(RecipeV2 recipe)
		{
			foreach (IngredientV2 ingredient in recipe.RequiredIngredients)
			{
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

		private static void ReAddRelatedRecipes(RecipeV2 recipe)
		{
			for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
			{
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