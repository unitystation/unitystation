using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Asset
{
	[Category(nameof(Asset))]
	public class TechWebDesignsTests
	{
		private static readonly string[] RequiredFields =
		{
			"Internal_ID", "name", "Item_ID", "Machinery_type"
		};

		private static string DesignsFolder =>
			Path.Combine(Application.dataPath, "StreamingAssets", "Config", "TechWeb", "Designs");

		private static List<(string file, Dictionary<string, object> entry)> LoadAllDesigns()
		{
			var designs = new List<(string, Dictionary<string, object>)>();
			foreach (var filePath in Directory.GetFiles(DesignsFolder, "*.json"))
			{
				var json = File.ReadAllText(filePath);
				var entries = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
				var fileName = Path.GetFileName(filePath);
				foreach (var entry in entries)
				{
					designs.Add((fileName, entry));
				}
			}

			return designs;
		}

		[Test]
		public void AllDesignFilesAreValidJson()
		{
			var report = new TestReport();

			foreach (var filePath in Directory.GetFiles(DesignsFolder, "*.json"))
			{
				var fileName = Path.GetFileName(filePath);
				var json = File.ReadAllText(filePath);
				try
				{
					JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
				}
				catch (JsonException e)
				{
					report.Fail().AppendLine($"{fileName}: Invalid JSON - {e.Message}");
				}
			}

			report.AssertPassed();
		}

		[Test]
		public void AllDesignsHaveRequiredFields()
		{
			var report = new TestReport();

			foreach (var (file, entry) in LoadAllDesigns())
			{
				var id = entry.ContainsKey("Internal_ID") ? entry["Internal_ID"].ToString() : "UNKNOWN";
				foreach (var field in RequiredFields)
				{
					report.FailIf(entry.ContainsKey(field) == false)
						.AppendLine($"{file} -> '{id}': Missing required field '{field}'");
				}
			}

			report.AssertPassed();
		}

		[Test]
		public void NoDuplicateInternalIDs()
		{
			var report = new TestReport();
			var seenIDs = new Dictionary<string, string>();

			foreach (var (file, entry) in LoadAllDesigns())
			{
				if (entry.ContainsKey("Internal_ID") == false) continue;

				var id = entry["Internal_ID"].ToString();
				report.FailIf(seenIDs.TryGetValue(id, out var existingFile))
					.AppendLine($"Duplicate Internal_ID '{id}' found in '{file}' and '{existingFile}'");

				seenIDs.TryAdd(id, file);
			}

			report.AssertPassed();
		}

		[Test]
		public void AllItemIDsReferenceValidPrefabs()
		{
			var report = new TestReport();
			var prefabs = Utils.FindPrefabs();
			var foreverIDs = new HashSet<string>();

			foreach (var prefab in prefabs)
			{
				if (prefab.TryGetComponent<US13.UI.Core.PrefabTracker>(out var tracker))
				{
					foreverIDs.Add(tracker.ForeverID);
				}
			}

			foreach (var (file, entry) in LoadAllDesigns())
			{
				if (entry.ContainsKey("Item_ID") == false) continue;
				if (entry.ContainsKey("Category") && entry["Category"].ToString().Contains("NonProducable")) continue;

				var itemID = entry["Item_ID"].ToString();
				var id = entry.ContainsKey("Internal_ID") ? entry["Internal_ID"].ToString() : "UNKNOWN";

				if (itemID.Contains("/"))
				{
					Debug.LogWarning($"{file} -> '{id}': Item_ID '{itemID}' is a TG placeholder path, skipping validation");
					continue;
				}

				report.FailIf(foreverIDs.Contains(itemID) == false)
					.AppendLine($"{file} -> '{id}': Item_ID '{itemID}' does not match any prefab's ForeverID");
			}

			report.AssertPassed();
		}
	}
}