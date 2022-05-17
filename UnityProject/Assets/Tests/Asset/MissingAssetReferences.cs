using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.IO;

namespace Tests.Asset
{
	/// <summary>
	/// Checks for missing references on prefabs and scriptable objects.
	/// TODO: These tests (both old and revised) have a strange issue causing them to take longer than they should
	/// sometimes. It may be caused by the SlotPopulatorEntry.Prefab exceeding serialization depth.
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
				foreach (var component in prefab.GetComponentsInChildren<Component>(true).Where(c => c == null))
				{
					var compType = Utils.GetObjectType(component);
					var compName = compType is null ? "component" : $"component ({compType.Name})";
					report.Fail()
						.Append($"{prefab.name} is missing a component. ")
						.Append($"The associated script for the {compName} could not be loaded.")
						.AppendLine();
				}
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
				Logger.Log(msg, Category.Tests);
				report.AppendLine(msg);
			}

			Assert.IsEmpty(listResults, report.ToString());
		}


		private void CheckMissingReferenceFieldsScriptableObjects(string path, ReferenceStatus status)
		{
			var report = new TestReport();
			var serializedObjectFieldsMap = new SerializedObjectFieldsMap();

			foreach (var so in Utils.FindAssetsByType<ScriptableObject>(path))
			{
				var missingRefs = serializedObjectFieldsMap
					.FieldNamesWithStatus(so, status)
					.ToList();

				var message = $"ScriptableObject ({so.name}) has serializable fields";
				message = status switch
				{
					ReferenceStatus.None => $"{message} that are None/Null: ",
					ReferenceStatus.Missing => $"{message} with a missing reference to a unity object: ",
					_ => string.Empty
				};

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
			CheckMissingReferenceFieldsScriptableObjects("Assets/ScriptableObjects",
				ReferenceStatus.Missing);
		}

		/// <summary>
		/// Check if there are singleton scriptable objects that have serializable object fields with no reference.
		/// </summary>
		[Test]
		public void TestSingletonScriptableObjects()
		{
			CheckMissingScriptableObjects("Resources/ScriptableObjectsSingletons");
			CheckMissingReferenceFieldsScriptableObjects("Assets/Resources/ScriptableObjectsSingletons",
				ReferenceStatus.None);
		}
	}
}
