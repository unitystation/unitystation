using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;

namespace Tests
{
	public class SubsceneTests
	{
		[Test]
		public void CheckMainStationInBuildSettings()
		{
			var report = new StringBuilder();

			if (!TryGetScriptableObjectGUID(typeof(MainStationListSO), report, out string asset))
			{
				Assert.Fail(report.ToString());
				return;
			}

			MainStationListSO mainStations = AssetDatabase.LoadAssetAtPath<MainStationListSO>(AssetDatabase.GUIDToAssetPath(asset));

			if (!CheckForScenesInBuildSettings(typeof(MainStationListSO), report, mainStations.MainStations))
			{
				Assert.Fail(report.ToString());
				return;
			}
		}

		[Test]
		public void CheckAwayWorldInBuildSettings()
		{
			var report = new StringBuilder();

			if (!TryGetScriptableObjectGUID(typeof(AwayWorldListSO), report, out string asset))
			{
				Assert.Fail(report.ToString());
				return;
			}

			AwayWorldListSO awayWorlds = AssetDatabase.LoadAssetAtPath<AwayWorldListSO>(AssetDatabase.GUIDToAssetPath(asset));

			if (!CheckForScenesInBuildSettings(typeof(AwayWorldListSO), report, awayWorlds.AwayWorlds))
			{
				Assert.Fail(report.ToString());
				return;
			}
		}

		[Test]
		public void CheckAsteroidInBuildSettings()
		{
			var report = new StringBuilder();

			if (!TryGetScriptableObjectGUID(typeof(AsteroidListSO), report, out string asset))
			{
				Assert.Fail(report.ToString());
				return;
			}

			AsteroidListSO asteroids = AssetDatabase.LoadAssetAtPath<AsteroidListSO>(AssetDatabase.GUIDToAssetPath(asset));

			if (!CheckForScenesInBuildSettings(typeof(AsteroidListSO), report, asteroids.Asteroids))
			{
				Assert.Fail(report.ToString());
				return;
			}
		}

		/// <summary>
		/// Get the GUID for the provided Type of ScriptableObject, expects only one to exist, writes errors to the StringBuilder.
		/// </summary>
		bool TryGetScriptableObjectGUID(Type scriptableObjectType, StringBuilder sb, out string assetGUID)
		{
			assetGUID = string.Empty;

			string typeString = scriptableObjectType.Name;
			string[] asset = AssetDatabase.FindAssets("t:" + typeString);

			if (!asset.Any())
			{
				sb.AppendLine($"{typeString}: Could not locate {typeString}.");
				return false;
			}

			if (asset.Length > 1)
			{
				sb.AppendLine($"{typeString}: More than one {typeString} exists.");
				return false;
			}

			assetGUID = asset.First();
			return true;
		}

		/// <summary>
		/// Checks that build settings contain all scenes in the provided list and that they are enabled, writes errors to the StringBuilder.
		/// </summary>
		bool CheckForScenesInBuildSettings(Type scriptableObjectType, StringBuilder sb, List<string> scenesToCheck)
		{
			Dictionary<string, EditorBuildSettingsScene> buildSettingFiles = new Dictionary<string, EditorBuildSettingsScene>();
			foreach (EditorBuildSettingsScene ebss in EditorBuildSettings.scenes)
			{
				buildSettingFiles.Add(Path.GetFileNameWithoutExtension(ebss.path), ebss);
			}
			bool success = true;
			string typeString = scriptableObjectType.Name;
			foreach (string scene in scenesToCheck)
			{
				if (!buildSettingFiles.TryGetValue(scene, out var buildScene))
				{
					success = false;
					sb.AppendLine($"{typeString}: {scene} scene is not in the Build Settings list.");
					continue;
				}
				else if (!buildScene.enabled)
				{
					success = false;
					sb.AppendLine($"{typeString}: {scene} scene is not enabled in the Build Settings list.");
					continue;
				}
			}
			return success;
		}
	}
}