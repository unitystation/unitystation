using System.Collections.Generic;
using System.Globalization;
using Core.Identity;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.Tools
{
	/// <summary>
	/// This tool is a single use tool that will migrate names and descriptions from the Attributes component to the new Identity component.
	/// </summary>
	public class MigrateToIdentity: ScriptableWizard
	{
		[Tooltip("Folder inside Assets where the prefabs we want to look for are located.")]
		public Object folderObject;
		private Vector2 logScrollPos;
		private readonly List<string> log = new();
		private int processedPrefabs = new();
		private int prefabsWithoutAttributes = new();

		[MenuItem("Tools/Migrations/Migrate to Identity")]
		private static void CreateWizard()
		{
			DisplayWizard<MigrateToIdentity>("Migrate to Identity", "Migrate");
		}

		public void OnGUI()
		{
			folderObject = EditorGUILayout.ObjectField("Folder", folderObject, typeof(Object), false);
			logScrollPos = EditorGUILayout.BeginScrollView(logScrollPos, GUILayout.Height(200));
			foreach (var text in log)
			{
				EditorGUILayout.LabelField(text);
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Processed " + processedPrefabs + " prefabs from " + folderObject.OrNull()?.name, EditorStyles.boldLabel);
			EditorGUILayout.LabelField(prefabsWithoutAttributes + " prefabs had identity but no Attributes, so they are named Unknown ", EditorStyles.boldLabel);
			if (GUILayout.Button("Migrate"))
			{
				OnWizardCreate();
			}
		}

		/// <summary>
		/// Reads all game objects in the Assets folder and returns them as an array.
		/// </summary>
		/// <returns></returns>
		private GameObject[] AllGameObjects()
		{
			string folder = AssetDatabase.GetAssetPath(folderObject);
			var guids = AssetDatabase.FindAssets("t:GameObject", new[] {folder});
			GameObject[] gameObjects = new GameObject[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				gameObjects[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			return gameObjects;
		}

		private void MigrateNameAndDescription(GameObject[] gameObjects)
		{
			foreach (var go in gameObjects)
			{
				log.Add("Processing " + go.name);
				var entityIdentity = go.GetComponent<SimpleIdentity>();
				var attributes = go.GetComponent<global::Attributes>();

				if (entityIdentity == null || attributes == null) continue;
				if (entityIdentity && attributes == null)
				{
					prefabsWithoutAttributes++;
				}

				entityIdentity.SetInitialName(attributes.ArticleName);
				entityIdentity.SetDescription(string.Empty ,BuildDescription(attributes.InitialDescription));
				EditorUtility.SetDirty(go);
				PrefabUtility.RecordPrefabInstancePropertyModifications(go);
				processedPrefabs++;
			}

			log.Add("Saving assets...");
			AssetDatabase.SaveAssets();
		}

		private string BuildDescription(string description)
		{
			if (string.IsNullOrEmpty(description))
			{

				var article = "a";
				if (description!.StartsWith("a", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("e", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("i", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("o", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("u", true, CultureInfo.InvariantCulture))
				{
					article = "an";
				}
				return "This is "+ article + " {0}.";
			}

			return description;
		}

		private void OnWizardCreate()
		{
			MigrateNameAndDescription(AllGameObjects());
		}
	}
}