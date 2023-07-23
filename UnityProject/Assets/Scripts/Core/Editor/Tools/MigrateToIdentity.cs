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
		private int processedPrefabs = 0;
		private int prefabsWithoutAttributes = 0;
		private int noComponents = 0;

		private Queue<GameObject> gameObjectsToProcess = new();
		private bool isProcessing = false;

		[MenuItem("Tools/Migrations/Migrate to Identity")]
		private static void CreateWizard()
		{
			DisplayWizard<MigrateToIdentity>("Migrate to Identity", "Migrate");
		}

		public void OnGUI()
		{
			if (isProcessing && gameObjectsToProcess.Count > 0)
			{
				var go = gameObjectsToProcess.Dequeue();
				MigrateNameAndDescription(go);

				if (gameObjectsToProcess.Count == 0)
				{
					isProcessing = false;
					log.Add("Migration finished!");
				}
			}

			folderObject = EditorGUILayout.ObjectField("Folder", folderObject, typeof(Object), false);
			logScrollPos = EditorGUILayout.BeginScrollView(logScrollPos, GUILayout.Height(200));
			foreach (var text in log)
			{
				EditorGUILayout.LabelField(text);
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Processed " + processedPrefabs + " prefabs from " + folderObject.OrNull()?.name,
				EditorStyles.boldLabel);
			EditorGUILayout.LabelField(
				prefabsWithoutAttributes + " prefabs had identity but no Attributes, so they are named Unknown ",
				EditorStyles.boldLabel);
			EditorGUILayout.LabelField(
				noComponents + " prefabs had neither component",
				EditorStyles.boldLabel);
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
			GameObject[] gameObjects = GetAllGameObjects();
			foreach (var go in gameObjects)
			{
				gameObjectsToProcess.Enqueue(go);
			}

			isProcessing = true;
		}

		private void MigrateNameAndDescription(GameObject go)
		{
			log.Add("Processing " + go.name);
			var entityIdentity = go.GetComponent<SimpleIdentity>();
			var attributes = go.GetComponent<global::Attributes>();

			if (entityIdentity == null || attributes == null)
			{
				log.Add($"{go.name} has no identity or attributes component!");
				noComponents++;
				return;
			}

			if (entityIdentity && attributes == null)
			{
				log.Add($"{go.name} has identity but no attributes component!");
				prefabsWithoutAttributes++;
			}

			entityIdentity.SetInitialName(attributes.InitialName);
			entityIdentity.SetDescription(BuildDescription(attributes.InitialDescription));
			EditorUtility.SetDirty(go);
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