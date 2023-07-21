using Core.Identity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEditorInternal;

namespace Core.Editor.Tools
{
	public class PrefabsWithoutIdentity: ScriptableWizard
	{
		[FormerlySerializedAs("path")] [Tooltip("Folder inside Assets where the prefabs we want to look for are located.")]
		public Object folderObject;

		private Vector2 logScrollPos;
		private Vector2 noIdentityScrollPos;
		private List<GameObject> noIdentityObjects = new();
		private List<string> log = new();

		[MenuItem("Tools/Migrations/Find Prefabs without Identity")]
		private static void CreateWizard()
		{
			DisplayWizard<PrefabsWithoutIdentity>("Find Prefabs without Identity", "Find");
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

			EditorGUILayout.LabelField("Prefabs without Identity", EditorStyles.boldLabel);
			noIdentityScrollPos = EditorGUILayout.BeginScrollView(noIdentityScrollPos, GUILayout.Height(200));
			EditorGUILayout.BeginVertical();
			for (int i = 0; i < noIdentityObjects.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.ObjectField(noIdentityObjects[i], typeof(GameObject), false);
				if (GUILayout.Button("Add EntityIdentity", GUILayout.Width(120)))
				{
					AddEntityIdentity(noIdentityObjects[i]);
					noIdentityObjects.RemoveAt(i);
					--i;
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			EditorGUILayout.LabelField("Found " + noIdentityObjects.Count + " prefabs without identity inside " + folderObject.OrNull()?.name, EditorStyles.boldLabel);

			if (GUILayout.Button("Find"))
			{
				OnWizardCreate();
			}
		}

		private void OnWizardCreate()
		{
			log.Clear();
			noIdentityObjects.Clear();
			string folder = AssetDatabase.GetAssetPath(folderObject);
			log.Add("Running!");
			var guids = AssetDatabase.FindAssets("t:GameObject", new[] {folder});
			var gameObjects = new GameObject[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				gameObjects[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			foreach (var go in gameObjects)
			{
				log.Add($"Testing {go.name}...");
				if (go.GetComponent<EntityIdentity>() == null)
				{
					noIdentityObjects.Add(go);
				}
			}
		}

		private void AddEntityIdentity(GameObject go)
		{
			var entityIdentity = go.AddComponent<EntityIdentity>();
			int componentsCount = go.GetComponents<Component>().Length;
			for (int i = 1; i < componentsCount - 2; i++)
			{
				ComponentUtility.MoveComponentUp(entityIdentity);
			}

			EditorUtility.SetDirty(go);
			AssetDatabase.SaveAssets();
			log.Add("EntityIdentity added to " + go.name);
			Selection.activeObject = go;
		}
	}
}
