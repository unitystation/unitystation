#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Systems.CraftingV2.CustomUnityEditors
{
	/// <summary>
	/// 	The custom recipe editor that automates some fields and makes your life a bit easier..
	/// </summary>
	[CustomEditor(typeof(CraftingRecipe))]
	public class RecipeEditor : Editor
	{
		private readonly List<RecipeIngredient> lastSerializedIngredients = new List<RecipeIngredient>();
		private bool lastSerializedIsSimple;

		private CraftingRecipe recipe;
		private SerializedProperty spRecipeName;
		private SerializedProperty spRecipeDescription;
		private SerializedProperty spCategory;
		private SerializedProperty spCraftingTime;
		private SerializedProperty spRecipeIconOverride;
		private SerializedProperty spRequiredIngredients;
		private SerializedProperty spRequiredReagents;
		private SerializedProperty spRequiredToolTraits;
		private SerializedProperty spResult;
		private SerializedProperty spResultHandlers;
		private SerializedProperty spIsSimple;
		private SerializedProperty spIndexInSingleton;

		private void OnEnable()
		{
			spRecipeName = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.RecipeName))
			);
			spRecipeDescription = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.RecipeDescription))
			);
			spRecipeIconOverride = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.RecipeIconOverride))
			);
			spCategory = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.Category))
			);
			spCraftingTime = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.CraftingTime))
			);
			spRequiredIngredients = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.RequiredIngredients))
			);
			spRequiredToolTraits = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.RequiredToolTraits))
			);
			spRequiredReagents = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.RequiredReagents))
			);
			spResult = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.Result))
			);
			spResultHandlers = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.ResultHandlers))
			);
			spIsSimple = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.IsSimple))
			);
			spIndexInSingleton = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipe.IndexInSingleton))
			);


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
			EditorGUILayout.PropertyField(spRecipeDescription);
			EditorGUILayout.PropertyField(spRecipeIconOverride);
			EditorGUILayout.PropertyField(spCategory);
			EditorGUILayout.PropertyField(spCraftingTime);
			EditorGUILayout.PropertyField(spRequiredIngredients);
			EditorGUILayout.PropertyField(spRequiredToolTraits);
			EditorGUILayout.PropertyField(spRequiredReagents);
			EditorGUILayout.PropertyField(spResult);
			EditorGUILayout.PropertyField(spResultHandlers);
			EditorGUILayout.PropertyField(spIsSimple);
			EditorGUILayout.PropertyField(spIndexInSingleton);

			if (GUILayout.Button("Add to the singleton if necessary"))
			{
				AddToSingletonIfNecessary();
			}

			if (GUILayout.Button("Fix serialized fields"))
			{
				serializedObject.Update();
				serializedObject.ApplyModifiedProperties();
				UpdateRelatedRecipes();
				UpdateSelfIsSimple();
				UpdateLastSerializedIngredients();
				UpdateLastSerializedIsSimple();
			}

			if (GUILayout.Button("Prepare for deletion"))
			{
				PrepareForDeletion();
			}

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				UpdateRelatedRecipesIfNecessary();
				UpdateSelfIsSimple();
				UpdateLastSerializedIngredients();
				UpdateLastSerializedIsSimple();
			}
		}

		#region SelfUpdates

		/// <summary>
		/// 	Clears the recipe's required ingredients list
		/// 	(so we can update the required ingredients' related recipes later).
		/// 	Removes the recipe from the recipe singleton.
		/// </summary>
		private void PrepareForDeletion()
		{
			recipe.RequiredIngredients.Clear();
			RemoveFromSingleton();
			serializedObject.Update();
		}

		/// <summary>
		/// 	Remove the recipe from the recipe singleton.
		/// </summary>
		private void RemoveFromSingleton()
		{
			CraftingRecipeSingleton.Instance.RemoveRecipe(recipe);
			recipe.IndexInSingleton = -1;
		}

		/// <summary>
		/// 	Adds the recipe to the singleton if necessary.
		/// </summary>
		private void AddToSingletonIfNecessary()
		{
			if (CraftingRecipeSingleton.Instance.AddRecipeIfNecessary(recipe))
			{
				serializedObject.Update();
			}
		}

		/// <summary>
		/// 	Updates the lastSerializedIngredients list.
		/// </summary>
		private void UpdateLastSerializedIngredients()
		{
			lastSerializedIngredients.Clear();
			foreach (RecipeIngredient requiredIngredient in recipe.RequiredIngredients)
			{
				lastSerializedIngredients.Add((RecipeIngredient) requiredIngredient.Clone());
			}
		}

		/// <summary>
		/// 	Updates the lastSerializedIsSimple field.
		/// </summary>
		private void UpdateLastSerializedIsSimple()
		{
			lastSerializedIsSimple = recipe.IsSimple;
		}

		/// <summary>
		/// 	Updates the current state of the isSimple field.
		/// </summary>
		private void UpdateSelfIsSimple()
		{
			recipe.IsSimple = recipe.RequiredIngredients.Count + recipe.RequiredToolTraits.Count == 2;
			serializedObject.Update();
		}

		#endregion

		#region RelatedRecipesUpdate

		/// <summary>
		/// 	Updates the recipe's required ingredients' related recipe list if necessary.
		/// </summary>
		private void UpdateRelatedRecipesIfNecessary()
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

			UpdateRelatedRecipes();
		}

		/// <summary>
		/// 	Updates the recipe's required ingredients' related recipe list.
		/// </summary>
		private void UpdateRelatedRecipes()
		{
			bool isSimpleFieldWasChanged = recipe.IsSimple != lastSerializedIsSimple;

			ClearRelatedRecipes(isSimpleFieldWasChanged);
			ReAddRelatedRecipes(isSimpleFieldWasChanged);
		}

		/// <summary>
		/// 	Removes the recipe from the recipe's required ingredient's relatedRecipes list.
		/// </summary>
		/// <param name="updateHasSimpleRelatedRecipe">
		/// 	Shall the method update recipe's required ingredients' hasSimpleRelatedRecipe?
		/// </param>
		private void ClearRelatedRecipes(bool updateHasSimpleRelatedRecipe)
		{
			foreach (RecipeIngredient recipeIngredient in lastSerializedIngredients)
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

		/// <summary>
		/// 	Adds the recipe to the required ingredient's relatedRecipes list.
		/// </summary>
		/// <param name="updateHasSimpleRelatedRecipe">
		///		Shall the method update recipe's required ingredients' hasSimpleRelatedRecipe?
		/// </param>
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

		#endregion
	}
}

#endif
