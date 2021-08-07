using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Tilemaps.Behaviours.Layers;
using Shuttles;
using Objects.Atmospherics;


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

					var matrixSyncs = gameObject.GetComponentsInChildren<MatrixSync>();

					//Make sure matrix has matrix sync
					if (matrixSyncs.Length == 0)
					{
						report.AppendLine($"{scene}: {gameObject.name} is missing a Matrix Sync, please add one");
						isok = false;
					}

					//Make matrix has only one matrix sync
					if (matrixSyncs.Length > 1)
					{
						report.AppendLine($"{scene}: {gameObject.name} has more than on matrix sync, only one is allowed");
						isok = false;
					}

					//Make sure matrix sync has correct parent
					foreach (var matrixSync in matrixSyncs)
					{
						if (matrixSync.transform.parent == gameObject.transform) continue;

						report.AppendLine($"{scene}: {matrixSync.gameObject.name} is parented to {matrixSync.transform.parent.gameObject.name}" +
						                  $", when it should be parented to {gameObject.name}");
						isok = false;
					}
				}
			}

			if (isok == false)
			{
				Assert.Fail(report.ToString());
			}
		}

		/// <summary>
		/// Checks to make sure all objects in the scenes with item storage have the force spawn set to true
		/// </summary>
		[Test]
		public void CheckItemStorageScene()
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

					foreach (ItemStorage child in gameObject.GetComponentsInChildren<ItemStorage>())
					{
						//Pickupable is fine to not check
						if (child.GetComponent<Pickupable>() != null) continue;

						//Only check stuff which has populator
						if (child.ItemStoragePopulator == null) continue;

						//Object should always force spawn
						if (child.forceSpawnContents) continue;

						//Currently objects should always force spawn as it can cause issues otherwise
						report.AppendLine($"{scene}: {child.name} is an object with a item storage with forceSpawnContents off, turn it on");
						isok = false;
					}
				}
			}

			if (isok == false)
			{
				Assert.Fail(report.ToString());
			}
		}

		/// <summary>
		/// Checks to make sure all objects in the prefabs with item storage have the force spawn set to true
		/// </summary>
		[Test]
		public void CheckItemStoragePrefab()
		{
			bool isok = true;
			var report = new StringBuilder();
			var prefabGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});
			var prefabPaths = prefabGUIDs.Select(AssetDatabase.GUIDToAssetPath);

			foreach (var prefab in prefabPaths)
			{
				var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);

				if(gameObject == null) continue;

				//Pickupable is fine to not check
				if (gameObject.GetComponent<Pickupable>() != null) continue;

				foreach (var child in gameObject.GetComponents<ItemStorage>())
				{
					//Only check stuff which has populator
					if(child.ItemStoragePopulator == null) continue;

					//Object should always force spawn
					if (child.forceSpawnContents) continue;

					//Currently objects should always force spawn as it can cause issues otherwise
					report.AppendLine($"{prefab}: {gameObject.name} is an object with a item storage with forceSpawnContents off, turn it on");
					isok = false;
				}
			}

			if (isok == false)
			{
				Assert.Fail(report.ToString());
			}
		}


		/// <summary>
		/// Checks for duplicated Pipes or cables
		/// </summary>
		[Test]
		public void CheckPipesAndCables()
		{
			bool isok = true;
			var report = new StringBuilder();
			var scenesGUIDs = AssetDatabase.FindAssets("t:Scene");
			var scenesPaths = scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath);
			List<ElectricalCableTile> Cables = new List<ElectricalCableTile>();

			foreach (var scene in scenesPaths)
			{
				if (scene.Contains("DevScenes") || scene.StartsWith("Packages")) continue;

				var Openedscene = EditorSceneManager.OpenScene(scene);
				//report.AppendLine($"Checking {scene}");
				//Logger.Log($"Checking {scene}", Category.Tests);
				var Matrices =  UnityEngine.Object.FindObjectsOfType<UnderFloorLayer>();

				foreach (var Underfloor in Matrices)
				{
					BoundsInt bounds = Underfloor.Tilemap.cellBounds;

					for (int n = bounds.xMin; n < bounds.xMax; n++)
					{
						for (int p = bounds.yMin; p < bounds.yMax; p++)
						{
							Cables.Clear();
							Vector3Int localPlace = (new Vector3Int(n, p, 0));
							bool[] PipeDirCheck = new bool[4];

							for (int i = 0; i < 50; i++)
							{
								localPlace.z = -i + 1;
								var getTile = Underfloor.Tilemap.GetTile(localPlace) as LayerTile;
								if (getTile != null)
								{
									var electricalCableTile = getTile as ElectricalCableTile;
									if (electricalCableTile != null)
									{
										if (Cables.Contains(electricalCableTile))
										{
											isok = false;
											report.AppendLine(
												$"A Duplicate cables at ({n}, {p}) in {Underfloor.matrix.gameObject.scene.name} - {Underfloor.matrix.name} with another Cable Name -> {electricalCableTile.name}");

											Underfloor.Tilemap.SetTile(localPlace, null);
											Underfloor.Tilemap.SetColor(localPlace,Color.white);
											Underfloor.Tilemap.SetTransformMatrix(localPlace, Matrix4x4.identity);
										}
										Cables.Add(electricalCableTile);
									}

									var pipeTile = getTile as PipeTile;
									if (pipeTile != null)
									{
										var matrixStruct = Underfloor.Tilemap.GetTransformMatrix(localPlace);
										var connection = PipeTile.GetRotatedConnection(pipeTile, matrixStruct);
										var pipeDir = connection.Directions;
										for (var d = 0; d < pipeDir.Length; d++)
										{
											if (pipeDir[d].Bool)
											{
												if (PipeDirCheck[d])
												{
													isok = false;
													report.AppendLine(
														$"A pipe is overlapping its connection at ({n}, {p}) in {Underfloor.matrix.gameObject.scene.name} - {Underfloor.matrix.name} with another pipe");
													Underfloor.Tilemap.SetTile(localPlace, null);
													Underfloor.Tilemap.SetColor(localPlace,Color.white);
													Underfloor.Tilemap.SetTransformMatrix(localPlace, Matrix4x4.identity);
													break;
												}
												PipeDirCheck[d] = true;
											}
										}
									}
								}
							}
						}
					}

				}

				EditorApplication.SaveScene();
			}

			if (isok == false)
			{
				Assert.Fail(report.ToString());
			}
		}
	}
}
