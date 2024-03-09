using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.IO;
using Logs;

namespace Tests.Asset
{
	/// <summary>
	/// Checks for missing references on prefabs and scriptable objects.
	/// </summary>
	[Category(nameof(Asset))]
	public class MissingAssetReferences
	{
		/// <summary>
		/// Checks if there is any prefab with components that could not be loaded.
		/// </summary>
		[Test]
		public void CheckMissingComponentOnPrefabs()
		{
			var report = new TestReport();

			foreach (var prefab in Utils.FindPrefabs(false))
			{
				var hasNullComponents = prefab.GetComponentsInChildren<Component>(true).Any(c => c == null);

				report.FailIf(hasNullComponents)
					.AppendLine($"The script for a component on {prefab.name} could not be loaded.");
			}

			report.AssertPassed();
		}

		/// <summary>
		/// Checks if there is any prefab with missing references in its components.
		/// </summary>
		[Test]
		public void CheckMissingReferenceFieldsOnPrefabs()
		{
			var report = new TestReport();
			var serializedObjectFieldsMap = new SerializedObjectFieldsMap();

			foreach (var prefab in Utils.FindPrefabs(false))
			{
				foreach (var component in prefab.GetComponentsInChildren<Component>(true).NotNull())
				{
					var compType = component.GetType();
					var missingRefs = serializedObjectFieldsMap
						.FieldNamesWithStatus(component, ReferenceStatus.Missing)
						.Select(pair => pair.name)
						.ToList();

					report.FailIf(missingRefs.Any())
						.AppendLine($"Prefab ({prefab.name}) has missing references in component {compType.Name}: ")
						.AppendLineRange(missingRefs, "\tField: ");
				}
			}

			report.AssertPassed();
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
				Loggy.Log(msg, Category.Tests);
				report.AppendLine(msg);
			}

			Assert.IsEmpty(listResults, report.ToString());
		}

		private void CheckMissingOrNullReferenceFieldsSpriteSOs()
		{
			var report = new TestReport();
			var FieldInfo =  typeof(SpriteDataSO.Frame).GetField("sprite");
			List<string> Missing = new List<string>();
			foreach (var so in Utils.FindAssetsByType<SpriteDataSO>(""))
			{
				for (int i = 0; i < so.Variance.Count; i++)
				{
					for (int j = 0; j < so.Variance[i].Frames.Count; j++)
					{
						var Status =  SerializedObjectFieldsMap.GetReferenceStatus(FieldInfo, so.Variance[i].Frames[j]);
						switch (Status)
						{
								case ReferenceStatus.Null:
									Missing.Add( $"{so.name} Index {i} Subindex {j} is None/Null");
									break;
								case ReferenceStatus.Missing:
									Missing.Add($"{so.name} Index {i} Subindex {j} has a missing reference.");
									break;
								default:
									break;
						}
					}
				}
			}


			report.FailIf(Missing.Any());
			foreach (var Missed in Missing)
			{
				report.AppendLine(Missed);
			}
			report.AssertPassed();
		}

		private void CheckMissingOrNullReferenceFieldsScriptableObjects(string path, ReferenceStatus status)
		{
			var report = new TestReport();
			var serializedObjectFieldsMap = new SerializedObjectFieldsMap();

			foreach (var so in Utils.FindAssetsByType<ScriptableObject>(path))
			{
				var missingRefs = serializedObjectFieldsMap
					.FieldNamesWithStatus(so, status)
					.Select(pair => pair.status switch
						{
							ReferenceStatus.Null => $"{pair.name} is None/Null",
							ReferenceStatus.Missing => $"{pair.name} has a missing reference.",
							_ => string.Empty
						}
					)
					.ToList();

				var message = $"ScriptableObject ({so.name}) has invalid serializable Unity object fields: ";

				report.FailIf(missingRefs.Any())
					.AppendLine(message)
					.AppendLineRange(missingRefs, "\tField: ");
			}

			report.AssertPassed();
		}

		/// <summary>
		/// Check if there are scriptable objects that have serializable object fields with a missing reference.
		/// </summary>
		[Test]
		public void TestScriptableObjects()
		{
			CheckMissingScriptableObjects("ScriptableObjects");
			CheckMissingOrNullReferenceFieldsScriptableObjects("ScriptableObjects",
				ReferenceStatus.Missing);
		}

		[Test]
		public void TestScriptableObjectsSprites()
		{
			CheckMissingOrNullReferenceFieldsSpriteSOs();
		}

		/// <summary>
		/// Check if there are singleton scriptable objects that have serializable object fields with no reference.
		/// </summary>
		[Test]
		public void TestSingletonScriptableObjects()
		{
			CheckMissingScriptableObjects("Resources/ScriptableObjectsSingletons");
			CheckMissingOrNullReferenceFieldsScriptableObjects("Resources/ScriptableObjectsSingletons",
				ReferenceStatus.Null | ReferenceStatus.Missing);
		}
	}
}
