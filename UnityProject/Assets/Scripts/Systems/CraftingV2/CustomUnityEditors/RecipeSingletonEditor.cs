#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Systems.CraftingV2.CustomUnityEditors
{
	[CustomEditor(typeof(CraftingRecipeSingleton))]
	public class RecipeSingletonEditor : Editor
	{
		private CraftingRecipeSingleton singleton;

		private SerializedProperty spStoredCraftingRecipes;

		public void OnEnable()
		{
			singleton = (CraftingRecipeSingleton) target;

			spStoredCraftingRecipes = serializedObject.FindProperty(
				Utils.Title2Camel(singleton.GetStoredCraftingRecipesListFieldName())
			);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(spStoredCraftingRecipes);

			if (GUILayout.Button("Remove nulls and fix recipes' indexes"))
			{
				RemoveNulls();
				FixRecipeIndexes();
			}

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void RemoveNulls()
		{
			singleton.RemoveNulls();
			serializedObject.Update();
		}

		private void FixRecipeIndexes()
		{
			singleton.FixRecipeIndexes();
		}
	}
}
#endif
