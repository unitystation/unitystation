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
			spStoredCraftingRecipes = serializedObject.FindProperty(
				Utils.Title2Camel(nameof(CraftingRecipeSingleton.StoredCraftingRecipes))
			);

			singleton = (CraftingRecipeSingleton) target;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(spStoredCraftingRecipes);

			// damn I want a button like this for every problem :(
			if (GUILayout.Button("Autoresolve all problems"))
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
			for (int i = singleton.StoredCraftingRecipes.Count - 1; i >= 0; i--)
			{
				if (singleton.StoredCraftingRecipes[i] == null)
				{
					singleton.StoredCraftingRecipes.RemoveAt(i);
				}
			}
			serializedObject.Update();
		}

		private void FixRecipeIndexes()
		{
			for (int i = 0; i < singleton.StoredCraftingRecipes.Count; i++)
			{
				singleton.StoredCraftingRecipes[i].IndexInSingleton = i;
				EditorUtility.SetDirty(singleton.StoredCraftingRecipes[i]);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}
#endif
