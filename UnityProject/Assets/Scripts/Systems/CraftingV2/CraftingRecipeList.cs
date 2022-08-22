using System;
using System.Collections.Generic;
using System.Linq;
using Systems.CraftingV2;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace Systems.CraftingV2
{
	/// <summary>
	/// A list of crafting recipes
	/// </summary>
	[CreateAssetMenu(fileName = "CraftingRecipeList", menuName = "ScriptableObjects/Crafting/CraftingRecipeList")]
	public class CraftingRecipeList : ScriptableObject
	{
		[SerializeField] [Tooltip("List of crafting recipies")]
		private List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();
		public List<CraftingRecipe> CraftingRecipes => craftingRecipes;

#if UNITY_EDITOR
		[ContextMenu("Add All Recipes")]
		private void AddAllRecipes()
		{
			var prefabGUIDS = AssetDatabase.FindAssets("t:CraftingRecipe");
			foreach (var prefabGUID in prefabGUIDS)
			{
				var path = AssetDatabase.GUIDToAssetPath(prefabGUID);
				var toCheck = AssetDatabase.LoadMainAssetAtPath(path) as CraftingRecipe;
				if(toCheck == null) continue;

				craftingRecipes.Add(toCheck);
			}
		}

		public void RemoveNullsInDefaultKnownRecipes()
		{
			for (int i = craftingRecipes.Count - 1; i >= 0; i--)
			{
				if (craftingRecipes[i] == null)
				{
					craftingRecipes.RemoveAt(i);
				}
			}
		}

		public void RemoveDuplicatesInDefaultKnownRecipes()
		{
			var checkHashset = new HashSet<CraftingRecipe>();
			var duplicate = false;

			foreach (var recipe in craftingRecipes)
			{
				if (checkHashset.Contains(recipe) == false)
				{
					checkHashset.Add(recipe);
				}
				else if (recipe != null)
				{
					duplicate = true;
				}
			}

			if (duplicate)
			{
				craftingRecipes = checkHashset.ToList();

				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssets();
			}
		}
#endif
	}
}

#region UnityEditor
#if UNITY_EDITOR

[CustomEditor(typeof(CraftingRecipeList))]
public class PlayerCraftingEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("Remove nulls"))
		{
			((CraftingRecipeList) target).RemoveNullsInDefaultKnownRecipes();
			serializedObject.Update();
		}

		if (GUILayout.Button("Remove duplicates"))
		{
			((CraftingRecipeList) target).RemoveDuplicatesInDefaultKnownRecipes();
			serializedObject.Update();
		}
	}
}

#endif
#endregion