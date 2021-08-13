using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;

namespace Tests
{
	public class MissingAssetReferences
	{
		/// <summary>
		/// Checks if there is any prefab with missing reference component
		/// </summary>
		[Test]
		public void CheckMissingComponentOnPrefabs()
		{
			List<string> listResult = new List<string>();

			string[] allPrefabs = SearchAndDestroy.GetAllPrefabs();
			foreach (string prefab in allPrefabs)
			{
				Object o = AssetDatabase.LoadMainAssetAtPath(prefab);

				// Check if it's gameobject
				if (!(o is GameObject))
				{
					Logger.LogFormat("For some reason, prefab {0} won't cast to GameObject", Category.Tests, prefab);
					continue;
				}

				var go = (GameObject)o;
				Component[] components = go.GetComponentsInChildren<Component>(true);
				foreach (Component c in components)
				{
					if (c == null)
					{
						listResult.Add(prefab);
					}
				}
			}

			foreach (string s in listResult)
			{
				Logger.LogFormat("Missing reference found in prefab {0}", Category.Tests, s);
			}

			Assert.IsEmpty(listResult, "Missing references found: {0}", string.Join(", ", listResult));
		}

		/// <summary>
		/// Check if there any prefab with MissingReference field
		/// </summary>
		[Test]
		public void CheckMissingReferenceFieldsOnPrefabs()
		{
			// GameObject name - Component Name - Field Nme
			var listResult = new List<(string, string, string)>();

			string[] allPrefabs = SearchAndDestroy.GetAllPrefabs();
			foreach (string prefab in allPrefabs)
			{
				Object o = AssetDatabase.LoadMainAssetAtPath(prefab);

				// Check if it's gameobject
				if (!(o is GameObject))
				{
					Logger.LogFormat("For some reason, prefab {0} won't cast to GameObject", Category.Tests, prefab);
					continue;
				}

				var go = (GameObject)o;
				Component[] components = go.GetComponentsInChildren<Component>(true);
				foreach (Component c in components)
				{
					if (c == null)
						continue;

					var so = new SerializedObject(c);
					var missingRefs = GetMissingRefs(so);
					foreach (var miss in missingRefs)
						listResult.Add((go.name, c.name, miss));
				}
			}

			var report = new StringBuilder();
			foreach (var s in listResult)
			{
				var msg = $"Missing reference found in prefab {s.Item1} component {s.Item2} field {s.Item3}";
				Logger.Log(msg, Category.Tests);
				report.AppendLine(msg);
			}

			Assert.IsEmpty(listResult, report.ToString());
		}

		/// <summary>
		/// Check if there are scriptable objects that lost their script
		/// </summary>
		private void CheckMissingScriptableObjects(string sourceFolderPath)
		{
			// Get all assets paths
			var allResourcesPaths = AssetDatabase.GetAllAssetPaths()
				.Where(p => p.Contains(sourceFolderPath));

			// Find all .asset (almost always it is SO)
			var allAssetPaths = allResourcesPaths.Where(path => path.EndsWith(".asset")).ToArray();

			var listResults = new List<string>();
			foreach (var assetFilePath in allAssetPaths)
			{
				var asset = AssetDatabase.LoadMainAssetAtPath(assetFilePath);

				// if we can't load it - something bad happend with SO
				if (!asset)
					listResults.Add(assetFilePath);
			}

			// Form report
			var report = new StringBuilder();
			foreach (string brokenAssetFilePath in listResults)
			{
				var fileName = Path.GetFileName(brokenAssetFilePath);
				var msg = $"Can't load asset {fileName}. Maybe linked ScriptableObject script is missing?";
				Logger.Log(msg, Category.Tests);
				report.AppendLine(msg);
			}

			Assert.IsEmpty(listResults, report.ToString());
		}

		/// <summary>
		/// Check if there are scriptable objects that has missing reference fields
		/// </summary>
		private void CheckMissingReferenceFieldsScriptableObjects(string path, bool checkEmpty = false)
		{
			// Get all assets paths
			var allResourcesPaths = AssetDatabase.GetAllAssetPaths()
				.Where(p => p.Contains(path));

			// Find all .asset (almost always it is SO)
			var allAssetPaths = allResourcesPaths.Where((a) => a.EndsWith(".asset")).ToArray();

			var listResults = new List<(string, string)>();
			foreach (var lookUpPath in allAssetPaths)
			{
				var asset = AssetDatabase.LoadMainAssetAtPath(lookUpPath);

				// skip invalid assets
				if (!asset || !(asset is ScriptableObject))
					continue;

				var so = new SerializedObject(asset);
				var missRefs = GetMissingRefs(so, checkEmpty);
				foreach (var miss in missRefs)
					listResults.Add((asset.name, miss));
			}

			// Form report
			var report = new StringBuilder();
			foreach (var s in listResults)
			{
				var msg = $"Missing reference found in scriptable object {s.Item1} field {s.Item2}";
				Logger.Log(msg, Category.Tests);
				report.AppendLine(msg);
			}

			Assert.IsEmpty(listResults, report.ToString());
		}

		[Test]
		public void TestScriptableObjects()
		{
			CheckMissingScriptableObjects("ScriptableObjects");
			CheckMissingReferenceFieldsScriptableObjects("ScriptableObjects");
		}

		[Test]
		public void TestSingletonScriptableObjects()
		{
			CheckMissingScriptableObjects("Resources/ScriptableObjectsSingletons");
			CheckMissingReferenceFieldsScriptableObjects("Resources/ScriptableObjectsSingletons", true);
		}


		/// <summary>
		/// Check if there are missing components or reference fields in a scene
		/// Checks only scenes selected for build
		/// </summary>
		[Test]
		public void CheckMissingComponentsOnScenes()
		{
			var buildScenes = EditorBuildSettings.scenes;

			var missingComponentsReport = new List<(string, string)>();
			var missingFieldsReport = new List<(string, string, string, string)>();

			foreach (var scene in buildScenes)
			{
				var currentScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
				var currentSceneName = currentScene.name;

				var allGO = GameObject.FindObjectsOfType<GameObject>();
				foreach (var go in allGO)
				{
					Component[] components = go.GetComponents<Component>();
					foreach (Component c in components)
					{
						var parent = go.transform.parent;
						var parentName = parent ? parent.name +'/' : "";

						if (c == null)
						{
							missingComponentsReport.Add((currentSceneName, parentName + go.name));
						}
						else
						{
							var so = new SerializedObject(c);
							var missingRefs = GetMissingRefs(so);
							foreach (var miss in missingRefs)
								missingFieldsReport.Add((currentSceneName, parentName + go.name, c.name, miss));
						}
					}
				}
			}

			// Form report about missing components
			var report = new StringBuilder();
			foreach (var s in missingComponentsReport)
			{
				var missingComponentMsg = $"Missing component found in scene {s.Item1}, GameObject {s.Item2}";
				Logger.Log(missingComponentMsg, Category.Tests);
				report.AppendLine(missingComponentMsg);
			}

			Assert.IsEmpty(missingComponentsReport, report.ToString());

			// Form report about missing refs
			report = new StringBuilder();
			foreach (var s in missingFieldsReport)
			{
				var missingFieldsMsg = $"Missing reference found in scene {s.Item1}, GameObject {s.Item2}, Component {s.Item3}, FieldName {s.Item4}";
				Logger.Log(missingFieldsMsg, Category.Tests);
				report.AppendLine(missingFieldsMsg);
			}

			Assert.IsEmpty(missingFieldsReport, report.ToString());

		}

		private static List<string> GetMissingRefs(SerializedObject so, bool checkEmpty = false)
		{
			var sp = so.GetIterator();
			var listResult = new List<string>();

			while (sp.NextVisible(true))
			{
				if (sp.propertyType == SerializedPropertyType.ObjectReference)
				{
					if (checkEmpty)
					{
						if (sp.objectReferenceValue == null
						    || sp.objectReferenceInstanceIDValue == 0)
						{
							listResult.Add(sp.displayName);
						}

						continue;
					}

					if (sp.objectReferenceValue == null
					    && sp.objectReferenceInstanceIDValue != 0)
					{
						listResult.Add(sp.displayName);
					}
				}
			}

			return listResult;
		}
	}
}
