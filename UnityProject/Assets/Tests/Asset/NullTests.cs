using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Core.Editor.Attributes;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tests.Asset
{
	public class NullTests
	{
		/// <summary>
		/// Checks to make sure all objects in the prefabs with fields with NotNull are not null
		/// </summary>
		[Test]
		public void CheckNotNullPrefab()
		{
			var report = new StringBuilder();
			var prefabGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});
			var prefabPaths = prefabGUIDs.Select(AssetDatabase.GUIDToAssetPath);

			foreach (var prefab in prefabPaths)
			{
				var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);

				if(gameObject == null) continue;

				CheckNotNull(gameObject, report, "", true);
			}

			Assert.IsEmpty(report.ToString());
		}

		/// <summary>
		/// Checks to make sure all objects in the scenes with fields with NotNull are not null
		/// </summary>
		[Test]
		public void CheckNotNullScene()
		{
			var report = new StringBuilder();
			var scenesGUIDs = AssetDatabase.FindAssets("t:Scene", new string[] {"Assets/Scenes"});
			var scenesPaths = scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath);
			foreach (var scene in scenesPaths)
			{
				if (scene.Contains("DevScenes") || scene.StartsWith("Packages")) continue;

				var openScene = EditorSceneManager.OpenScene(scene);
				var gameObjects = openScene.GetRootGameObjects();

				foreach (var gameObject in gameObjects)
				{
					CheckNotNull(gameObject, report, openScene.name);
				}
			}

			Assert.IsEmpty(report.ToString());
		}

		private void CheckNotNull(GameObject toCheck, StringBuilder report, string scene, bool isPrefab = false)
		{
			var components = toCheck.GetComponents<MonoBehaviour>();

			foreach (var component in components)
			{
				var fields = component.GetType().GetFields();

				foreach (var field in fields)
				{
					if(Attribute.IsDefined(field, typeof(NotNullAttribute)) == false) continue;

					Debug.LogError($"{component.name} has attribute");

					if(field.GetValue(component) != null) continue;

					report.AppendLine(
						$"{toCheck.ExpensiveName()} {(isPrefab ? "prefab" : $"scene: {scene}")} has a null value on component: {component.name} field: {field.Name}");
				}
			}
		}
	}
}
