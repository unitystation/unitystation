using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shuttles;
using Tilemaps.Behaviours.Layers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tests
{
	public class SubsceneTests
	{
		[Test]
		public void CheckMainStationInBuildSettings()
		{
			var report = new StringBuilder();

			if (!Utils.TryGetScriptableObjectGUID(typeof(MainStationListSO), report, out string asset))
			{
				Assert.Fail(report.ToString());
				return;
			}

			MainStationListSO mainStations =
				AssetDatabase.LoadAssetAtPath<MainStationListSO>(AssetDatabase.GUIDToAssetPath(asset));

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

			if (!Utils.TryGetScriptableObjectGUID(typeof(AwayWorldListSO), report, out string asset))
			{
				Assert.Fail(report.ToString());
				return;
			}

			AwayWorldListSO awayWorlds =
				AssetDatabase.LoadAssetAtPath<AwayWorldListSO>(AssetDatabase.GUIDToAssetPath(asset));

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

			if (!Utils.TryGetScriptableObjectGUID(typeof(AsteroidListSO), report, out string asset))
			{
				Assert.Fail(report.ToString());
				return;
			}

			AsteroidListSO asteroids =
				AssetDatabase.LoadAssetAtPath<AsteroidListSO>(AssetDatabase.GUIDToAssetPath(asset));

			if (!CheckForScenesInBuildSettings(typeof(AsteroidListSO), report, asteroids.Asteroids))
			{
				Assert.Fail(report.ToString());
				return;
			}
		}

		/// <summary>
		/// Checks that build settings contain all scenes in the provided list and that they are enabled, writes errors to the StringBuilder.
		/// </summary>
		bool CheckForScenesInBuildSettings(Type scriptableObjectType, StringBuilder sb, List<string> scenesToCheck)
		{
			Dictionary<string, EditorBuildSettingsScene> buildSettingFiles =
				new Dictionary<string, EditorBuildSettingsScene>();
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


		/// <summary>
		/// Checks scenes for prefabs that have gone to 0,0 and another check for missing prefabs since the other component doesn't seem to work too well
		/// </summary>
		[Test]
		public void Check00prefabs()
		{
			bool isok = true;
			var report = new StringBuilder();
			var scenesGUIDs = AssetDatabase.FindAssets("t:Scene");
			var scenesPaths = scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath);
			foreach (var scene in scenesPaths)
			{
				if (scene.Contains("DevScenes") || scene.StartsWith("Packages")) continue;

				var Openedscene = EditorSceneManager.OpenScene(scene);
				//report.AppendLine($"Checking {scene}");
				//Logger.Log($"Checking {scene}", Category.Tests);
				var gameObjects = Openedscene.GetRootGameObjects();
				foreach (var gameObject in gameObjects)
				{
					var ObjectLaye = gameObject.GetComponentInChildren<ObjectLayer>();
					if (ObjectLaye == null) continue;
					int NumberOfChildren = ObjectLaye.transform.childCount;


					for (int i = 0; i < NumberOfChildren; i++)
					{
						var ChildObject = ObjectLaye.transform.GetChild(i);
						if (ChildObject.name.Contains("Missing Prefab"))
						{
							isok = false;
							report.AppendLine(
								$"{scene}: {ChildObject.name} Missing prefab");
						}


						if (ChildObject.localPosition.x == 0 &&
						    ChildObject.localPosition.y == 0)
						{
							isok = false;
							report.AppendLine(
								$"{scene}: {ChildObject} is at 0,0 Please update the prefab/update the map/revert ");
						}
					}
				}
			}

			if (isok == false)
			{
				Assert.Fail(report.ToString());
			}
		}

		/// <summary>
		/// Checks to make sure all matrixes have a matrix sync
		/// </summary>
		[Test]
		public void CheckMatrixSync()
		{
			bool isok = true;
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
					if(gameObject.GetComponent<NetworkedMatrix>() == null) continue;

					var hadMatrixSync = false;

					foreach (Transform child in gameObject.transform)
					{
						if (child.GetComponent<MatrixSync>() != null)
						{
							hadMatrixSync = true;
							break;
						}
					}

					if (hadMatrixSync == false)
					{
						report.AppendLine($"{scene}: {gameObject.name} is missing a Matrix Sync, please add one");
						isok = false;
					}
				}
			}

			if (isok == false)
			{
				Assert.Fail(report.ToString());
			}
		}
	}
}