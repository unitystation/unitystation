using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Core.Identity;

namespace Core.Editor.Tools
{
	public class MigrateToIdentity : ScriptableWizard
	{
		[Tooltip("Folder inside Assets where the prefabs we want to look for are located.")]
		public Object folderObject;

		private Vector2 logScrollPos;
		private readonly List<string> log = new();
		private int totalPrefabs = 0;
		private int processedPrefabs = 0;
		private int prefabsWithoutAttributes = 0;
		private int noComponents = 0;

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
			EditorGUILayout.LabelField("Report", EditorStyles.largeLabel);
			EditorGUILayout.LabelField($"Processed {processedPrefabs} out of {totalPrefabs} prefabs", EditorStyles.boldLabel);

			if (prefabsWithoutAttributes > 0)
			{
				EditorGUILayout.LabelField(
					prefabsWithoutAttributes + " prefabs had identity but no Attributes, so they are named Unknown ",
					EditorStyles.boldLabel);
			}

			if (noComponents > 0)
			{
				EditorGUILayout.LabelField(
					noComponents + " prefabs had neither component",
					EditorStyles.boldLabel);
			}

			if (GUILayout.Button("Migrate"))
			{
				OnWizardCreate();
			}
		}

		private GameObject[] GetAllGameObjects()
		{
			string folder = AssetDatabase.GetAssetPath(folderObject);
			var guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder });
			GameObject[] gameObjects = new GameObject[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				gameObjects[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			return gameObjects;
		}

		private void OnWizardCreate()
		{
			totalPrefabs = 0;
			processedPrefabs = 0;
			prefabsWithoutAttributes = 0;
			log.Clear();
			GameObject[] gameObjects = GetAllGameObjects();
			foreach (var go in gameObjects)
			{
				MigrateNameAndDescription(go);
			}
			AssetDatabase.SaveAssets();
			log.Add("Migration finished!");
		}

		private void MigrateNameAndDescription(GameObject go)
		{
			log.Add("Processing " + go.name);
			var entityIdentity = go.GetComponent<SimpleIdentity>();
			var attributes = go.GetComponent<global::Attributes>();

			totalPrefabs++;
			if (entityIdentity == null && attributes == null)
			{
				log.Add($"{go.name} has no identity or attributes component!");
				noComponents++;
				return;
			}

			if (entityIdentity && attributes == null)
			{
				log.Add($"{go.name} has identity but no attributes component!");
				prefabsWithoutAttributes++;
				return;
			}

			entityIdentity.SetInitialName(attributes.InitialName);
			entityIdentity.SetDescription(BuildDescription(attributes.InitialDescription));
			EditorUtility.SetDirty(go);
			PrefabUtility.RecordPrefabInstancePropertyModifications(go);
			processedPrefabs++;
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

				return "This is " + article + " {0}.";
			}

			return description;
		}
	}
}