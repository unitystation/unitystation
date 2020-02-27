using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.IO;

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
		[Test]
		public void CheckMissingScritpableObjects()
		{
			// Get all assets paths
			var allResourcesPaths = AssetDatabase.GetAllAssetPaths()
				.Where(p => p.Contains("Resources/"));

			// Find all .asset (almost always it is SO)
			var allAssetPaths = allResourcesPaths.Where((a) => a.EndsWith(".asset")).ToArray();

			var listResults = new List<string>();
			foreach (var path in allAssetPaths)
			{
				var asset = AssetDatabase.LoadMainAssetAtPath(path);

				// if we can't load it - something bad happend with SO
				if (!asset)
					listResults.Add(path);
			}

			// Form report
			var report = new StringBuilder();
			foreach (string s in listResults)
			{
				var fileName = Path.GetFileName(s);
				var msg = $"Can't load asset {fileName}. Maybe linked ScriptableObject script is missing?";
				Logger.Log(msg, Category.Tests);
				report.AppendLine(msg);
			}

			Assert.IsEmpty(listResults, report.ToString());
		}

		/// <summary>
		/// Check if there are scriptable objects that has missing reference fields 
		/// </summary>
		[Test]
		public void CheckMissingRefenceFieldsScritpableObjects()
		{
			// Get all assets paths
			var allResourcesPaths = AssetDatabase.GetAllAssetPaths()
				.Where(p => p.Contains("Resources/"));

			// Find all .asset (almost always it is SO)
			var allAssetPaths = allResourcesPaths.Where((a) => a.EndsWith(".asset")).ToArray();

			var listResults = new List<(string, string)>();
			foreach (var path in allAssetPaths)
			{
				var asset = AssetDatabase.LoadMainAssetAtPath(path);

				// skip invalid assets
				if (!asset || !(asset is ScriptableObject))
					continue;

				var so = new SerializedObject(asset);
				var missRefs = GetMissingRefs(so);
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


		private static List<string> GetMissingRefs(SerializedObject so)
		{
			var sp = so.GetIterator();
			var listResult = new List<string>();

			while (sp.NextVisible(true))
			{
				if (sp.propertyType == SerializedPropertyType.ObjectReference)
				{
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