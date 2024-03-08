using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests
{
	public class SubsceneTests
	{
		[Test]
		public void CheckMainStationsInBuildSettings() =>
			CheckScenesInBuildSettings<MainStationListSO>(so => so.MainStations);

		[Test]
		public void CheckAwayWorldsInBuildSettings() =>
			CheckScenesInBuildSettings<AwayWorldListSO>(so => so.AwayWorlds);

		[Test]
		public void CheckAsteroidsInBuildSettings() =>
			CheckScenesInBuildSettings<AsteroidListSO>(so => so.Asteroids);

		private void CheckScenesInBuildSettings<T>(Func<T, List<string>> getSceneNames) where T : ScriptableObject
		{
			var report = new TestReport();
			var scenesSO = Utils.GetSingleScriptableObject<T>(report);
			CheckForScenesInBuildSettings<T>(report, getSceneNames(scenesSO));
			report.AssertPassed();
		}

		/// <summary>
		/// Checks that build settings contain all scenes in the provided list and that they are enabled, writes errors to the StringBuilder.
		/// </summary>
		private void CheckForScenesInBuildSettings<T>(TestReport report, IEnumerable<string> scenesToCheck)
		{
			var typeName = typeof(T).Name;
			var buildSettings =
				EditorBuildSettings.scenes.ToDictionary(ebss => Path.GetFileNameWithoutExtension(ebss.path));

			foreach (var sceneName in scenesToCheck)
			{
				buildSettings.TryGetValue(sceneName, out var editorScene);
				bool? enabled = editorScene?.enabled;
				report.Clean()
					.FailIfNot(enabled.HasValue)
					.AppendLine($"{typeName}: {sceneName} scene is not in the Build Settings list.")
					.MarkDirtyIfFailed()
					.FailIfNot(enabled.GetValueOrDefault(false))
					.AppendLine($"{typeName}: {sceneName} scene is not enabled in the Build Settings list.");
			}
		}
	}
}
