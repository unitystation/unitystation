using System;
using System.Linq;
using NUnit.Framework;
using Objects.Atmospherics;
using Shuttles;
using Tilemaps.Behaviours.Layers;
using Tiles;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Tests.Scenes
{
	public class GeneralSceneTests : SceneTest
	{
		public GeneralSceneTests(SceneTestData data) : base(data)
		{
		}

		/// <summary>
		/// Checks scenes for prefabs that have gone to 0,0 and another check for missing prefabs since the other component doesn't seem to work too well
		/// </summary>
		[Test]
		public void PrefabsAreNotAt00()
		{
			foreach (var layer in RootObjects.ComponentsInChildren<ObjectLayer>().NotNull())
			{
				foreach (Transform transform in layer.transform)
				{
					var localPos = transform.localPosition;
					var objName = transform.name;
					var message = $"Object: \"{transform.HierarchyName()}\"";
					Report.FailIf(objName.Contains("Missing Prefab"))
						.AppendLine($"{message} is missing prefab")
						.FailIf(localPos.x == 0 && localPos.y == 0)
						.AppendLine($"{message} is at 0,0. Please update the prefab/update the map/revert.");
				}
			}

			Report.AssertPassed();
		}

		/// <summary>
		/// Checks to make sure all matrices have a matrix sync
		/// </summary>
		[Test]
		public void MatrixHasMatrixSyncAndIsCorrectParent()
		{
			using var pool = ListPool<MatrixSync>.Get(out var matrixSyncs);

			foreach (var matrix in RootObjects.Select(go => go.GetComponent<NetworkedMatrix>()).NotNull())
			{
				matrix.GetComponentsInChildren(matrixSyncs);
				var matrixName = matrix.transform.HierarchyName();

				Report.FailIf(matrixSyncs.Count, Is.EqualTo(0))
					.AppendLine($"\"{matrixName}\" is missing a MatrixSync, please add one")
					.FailIf(matrixSyncs.Count, Is.GreaterThan(1))
					.AppendLine($"\"{matrixName}\" has more than one MatrixSync, only one is allowed");

				//Make sure matrix sync has correct parent
				foreach (var matrixSync in matrixSyncs)
				{
					var transform = matrixSync.transform;
					var parent = transform.parent;
					Report.FailIfNot(parent, Is.EqualTo(matrix.transform))
						.Append($"\"{matrixSync.name}\" is parented to \"{parent.transform.HierarchyName()}\" ")
						.Append($"when it should be parented to \"{matrixName}\"")
						.AppendLine();
				}
			}

			Report.AssertPassed();
		}

		[Test]
		public void ItemStorageHasForcesSpawn()
		{
			var storages = RootObjects
				.Components<NetworkedMatrix>().NotNull()
				.ComponentsInChildren<ItemStorage>().NotNull();
			foreach (var storage in storages)
			{
				var name = storage.transform.HierarchyName();
				//Currently objects should always force spawn as it can cause issues otherwise
				Report.FailIf(IsStorageWithoutForceSpawn(storage))
					.AppendLine($"\"{name}\" is an object with a item storage with forceSpawnContents off, turn it on.");
			}

			Report.AssertPassed();
		}

		/// <summary>
		/// Checks if the storage is non-pickupable that has populators and is not always forced to spawn.
		/// </summary>
		private static bool IsStorageWithoutForceSpawn(ItemStorage storage) =>
			storage != null
			&& storage.GetComponent<Pickupable>() == null
			&& storage.ItemStoragePopulator != null
			&& storage.forceSpawnContents == false;


		/// <summary>
		/// Checks for duplicated Pipes or cables
		/// </summary>
		[Test]
		public void PipesAndCablesAreNotOverlappingOrDuplicate()
		{
			var matrices = RootObjects
				.ComponentsInChildren<Matrix>().NotNull()
				.Where(matrix => matrix.UnderFloorLayer != null);

			foreach (var matrix in matrices)
			{
				var tilemap = matrix.UnderFloorLayer.Tilemap;
				var bounds = tilemap.cellBounds;
				for (var x = bounds.xMin; x < bounds.xMax; x++)
				{
					for (var y = bounds.yMin; y < bounds.yMax; y++)
					{
						CheckPipeAndCableTiles(matrix, tilemap, x, y);
					}
				}
			}

			if (Scene.isDirty)
			{
				EditorSceneManager.SaveOpenScenes();
			}

			Report.AssertPassed();
		}

		private void CheckPipeAndCableTiles(Matrix matrix, Tilemap tilemap, int x, int y)
		{
			var foundCable = false;
			Span<bool> checkPipeDir = stackalloc bool[4];
			// Copern: What are these magic numbers? Why specifically -48 to 1?
			for (int z = -48; z < 2; z++)
			{
				var localPos = new Vector3Int(x, y, z);

				if (tilemap.GetTile(localPos) is not LayerTile layerTile) continue;

				switch (layerTile)
				{
					case ElectricalCableTile cableTile:
						HandleCableTile(cableTile, localPos);
						break;
					case PipeTile pipeTile:
						HandlePipeTile(pipeTile, localPos, checkPipeDir);
						break;
				}
			}

			void HandleCableTile(ElectricalCableTile cableTile, Vector3Int localPos)
			{
				if (foundCable == false)
				{
					foundCable = true;
					return;
				}

				Report.Fail()
					.Append($"Duplicate cable found at ({x}, {y}) in {matrix.name} ")
					.Append($"with another cable -> {cableTile.name}")
					.AppendLine();

				ResetTile(tilemap, localPos);
			}

			void HandlePipeTile(PipeTile pipeTile, Vector3Int localPos, Span<bool> isDirConnected)
			{
				var transformMatrix = tilemap.GetTransformMatrix(localPos);
				var connections = PipeTile.GetRotatedConnection(pipeTile, transformMatrix);
				var pipeDir = connections.Directions;
				for (var d = 0; d < pipeDir.Length; d++)
				{
					// Copern: Bool? What is Bool representing? "IsConnected"?
					if (pipeDir[d].Bool == false) continue;

					if (isDirConnected[d])
					{
						Report.Fail()
							.Append($"A pipe is overlapping its connection at ({x}, {y}) in {Scene.name} - ")
							.Append($"{matrix.name} with another pipe")
							.AppendLine();

						ResetTile(tilemap, localPos);
						break;
					}
					isDirConnected[d] = true;
				}
			}
		}

		private void ResetTile(Tilemap tilemap, Vector3Int localPos)
		{
			EditorSceneManager.MarkSceneDirty(Scene);
			tilemap.SetTile(localPos, null);
			tilemap.SetColor(localPos, Color.white);
			tilemap.SetTransformMatrix(localPos, Matrix4x4.identity);
		}

		[Test]
		public void GameObjectsDoNotHaveMissingReferences()
		{
			var serializedObjectFieldsMap = new SerializedObjectFieldsMap();
			foreach (var go in Object.FindObjectsOfType<GameObject>(true))
			{
				foreach (var comp in go.GetComponents<Component>())
				{
					var name = go.transform.HierarchyName();

					// A missing component is always a true null.
					if (comp == null)
					{
						Report.Fail().AppendLine($"The script for a component on \"{name}\" could not be loaded.");
						continue;
					}

					var missingRefs = serializedObjectFieldsMap.FieldNamesWithStatus(comp, ReferenceStatus.Missing)
						.ToList();

					Report.FailIf(missingRefs.Count, Is.GreaterThan(0))
						.AppendLine($"\"{name}\" has missing references in component \"{comp.GetType().Name}\": ")
						.AppendLineRange(missingRefs, "\tField: ");
				}
			}

			Report.AssertPassed();
		}
	}
}