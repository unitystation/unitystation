using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Text;

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
		/// Check if there any field with MissingReference
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

					SerializedObject so = new SerializedObject(c);
					var sp = so.GetIterator();

					while (sp.NextVisible(true))
					{
						if (sp.propertyType == SerializedPropertyType.ObjectReference)
						{
							if (sp.objectReferenceValue == null
								&& sp.objectReferenceInstanceIDValue != 0)
							{
								listResult.Add((go.name, c.name, sp.displayName));
							}
						}
					}
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
	}
}