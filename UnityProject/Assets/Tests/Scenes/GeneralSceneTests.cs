using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;
using US13.Core.Lighting;
using US13.Objects;
using US13.Objects.Doors;
using US13.Objects.Engineering;
using US13.Objects.Pipes;
using US13.Objects.Pipes.Devices;
using US13.Objects.Wallmounts.Switches;
using US13.Shuttles;
using US13.Systems.Electricity;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Systems.Fluids;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using Util;
using Object = UnityEngine.Object;
using PipeLayer = US13.Tilemaps.Behaviours.Layers.PipeLayer;

namespace Tests.Scenes
{
	[Category(nameof(Scenes))]
	public class GeneralSceneTests : SceneTest
	{
		public GeneralSceneTests(SceneTestData data) : base(data)
		{
		}


		[Test]
		public void ItemStorageHasForcesSpawn()
		{
			foreach (var storage in RootObjects.ComponentsInChildren<ItemStorage>().NotNull())
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
			CheckPipesAndCablesForLayer<UnderFloorLayer>();
			CheckPipesAndCablesForLayer<ElectricalLayer>();
			CheckPipesAndCablesForLayer<PipeLayer>();
			CheckPipesAndCablesForLayer<DisposalsLayer>();

			if (Scene.isDirty)
			{
				EditorSceneManager.SaveOpenScenes();
			}

			Report.AssertPassed();
		}

		private void CheckPipesAndCablesForLayer<T>() where T : Layer
		{
			foreach (var layer in RootObjects.ComponentInChildren<T>().NotNull())
			{
				var tilemap = layer.Tilemap;
				var bounds = tilemap.cellBounds;
				for (var x = bounds.xMin; x < bounds.xMax; x++)
				{
					for (var y = bounds.yMin; y < bounds.yMax; y++)
					{
						CheckPipeAndCableTiles(layer.Matrix, tilemap, x, y);
					}
				}
			}
		}

		private void CheckPipeAndCableTiles(Matrix matrix, Tilemap tilemap, int x, int y)
		{
			using var pool = ListPool<ElectricalCableTile>.Get(out var cables);
			Span<bool> checkPipeDir = stackalloc bool[4];

			// The -48 to 2 refer to the Z axis of the tilemap. Some tiles can overlap themselves on the Z axis accidentally
			// Because unity's tilemap does not have a proper way to prevent this from happening.
			for (int z = -48; z < 2; z++)
			{
				var localPos = new Vector3Int(x, y, z);

				if (tilemap.GetTile(localPos) is not LayerTile layerTile) continue;

				if (layerTile is ElectricalCableTile cableTile)
				{
					HandleCableTile(cableTile, localPos);
				}
				else if (layerTile is PipeTile pipeTile)
				{
					HandlePipeTile(pipeTile, localPos, checkPipeDir);
				}
			}

			void HandleCableTile(ElectricalCableTile cableTile, Vector3Int localPos)
			{
				if (cables.Contains(cableTile))
				{
					Report.Fail()
						.Append($"Duplicate cable found at ({x}, {y}) in {Scene.name} - {matrix.name} ")
						.Append($"with another cable -> {cableTile.name}")
						.AppendLine();

					ResetTile(tilemap, localPos);
				}
				cables.Add(cableTile);
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
						.Select(pair => pair.name)
						.ToList();

					Report.FailIf(missingRefs.Count, Is.GreaterThan(0))
						.AppendLine($"\"{name}\" has missing references in component \"{comp.GetType().Name}\": ")
						.AppendLineRange(missingRefs, "\tField: ");
				}
			}

			Report.AssertPassed();
		}

			/// <summary>
		/// Finds all APCs
		/// Checks if Device list is empty
		/// if there are null values in the list
		/// if device is not assigned to this APC
		/// </summary>
		[Test]
		public void APCPoweredDevicesHaveRelatedAPC()
		{
			foreach (var device in RootObjects.ComponentsInChildren<APCPoweredDevice>().NotNull())
			{
				if (device.IsSelfPowered) continue;
				if (device.MappingNotNeedToLink) continue;
				if (device.GetComponentInChildren<AutoAPCLinker>() != null) continue;

				var deviceLocation = device.transform.NameAndPosition();
				var relatedAPC = device.RelatedAPC;

				if (relatedAPC == null)
				{
					Report.Fail().AppendLine($"{Scene.name}: {deviceLocation} has a missing APC reference");
					continue;
				}

				var apcLocation = relatedAPC.transform.NameAndPosition("APC");
				Report.FailIf(relatedAPC.ConnectedDevices.Contains(device) == false)
					.AppendLine($"{Scene.name}: {deviceLocation} is connected to ")
					.AppendLine($"{apcLocation} but the APC doesn't have this device.");
			}

			Report.AssertPassed();
		}

		/// <summary>
		/// Finds all APCs
		/// Checks if Device list is empty
		/// if there are null values in the list
		/// if device is not assigned to this APC
		/// </summary>
		[Test]
		public void APCsConnectedDevicesContainsValidReferences()
		{
			var sceneName = Scene.name;
			foreach (var apc in RootObjects.ComponentsInChildren<APC>().NotNull())
			{
				var apcTransform = apc.transform;

				foreach (var (connectedDevice, index) in apc.ConnectedDevices.WithIndex())
				{
					var apcLocation = apcTransform.NameAndPosition("APC");

					if (connectedDevice == null)
					{
						Report.Fail()
							.AppendLine($"{sceneName}: {apcLocation} has a null value in the list at index {index}.");
						continue;
					}

					var relatedAPC = connectedDevice.RelatedAPC;

					Report.FailIfNot(relatedAPC, Is.EqualTo(apc))
						.Append($"{sceneName}: {connectedDevice.transform.NameAndPosition("Device")} ")
						.Append($"is not connected to {apcLocation}.")
						.AppendLine();

					var currentAPC = "nothing";

					if (relatedAPC != null)
					{
						currentAPC = $"{relatedAPC.transform.NameAndPosition()}";
						Report.Append("The APC's devices list may unintentionally contain this device. ");
					}

					Report.Append($"The device is currently connected to {currentAPC}.")
						.AppendLine();
				}
			}

			Report.AssertPassed();
		}

		[Test]
		public void StatusDisplaysDoNotHaveNullDoors()
		{
			foreach (var display in RootObjects.ComponentsInChildren<StatusDisplay>().NotNull())
			{
				var position = display.transform.position;
				foreach (var doorController in display.NewdoorControllers)
				{
					Report.FailIf(doorController, Is.Null)
						.AppendLine($"{Scene.name}: \"{display.name}\" at {position} has a null {nameof(DoorMasterController)}.");
				}
			}

			Report.AssertPassed();
		}

		[Test]
		public void LightSourcesDoNotHaveMissingSwitch()
		{
			foreach (var lightSource in RootObjects.ComponentsInChildren<LightSource>().NotNull())
			{
				if (lightSource.IsWithoutSwitch) continue;

				var position = lightSource.transform.position;
				Report.FailIf(lightSource.relatedLightSwitch, Is.Null)
					.AppendLine($"{Scene.name}: \"{lightSource.name}\" at {position} has a missing switch reference.");
			}

			Report.AssertPassed();
		}

		[Test]
		public void LightSwitchesHaveLightSources()
		{
			foreach (var lightSwitch in RootObjects.ComponentsInChildren<LightSwitchV2>().NotNull())
			{
				var position = lightSwitch.transform.position;
				Report.FailIf(lightSwitch.listOfLights.Count, Is.EqualTo(0))
					.AppendLine($"{Scene.name}: \"{lightSwitch.name}\" at {position} has no light sources.");
			}

			Report.AssertPassed();
		}

				/// <summary>
		/// Checks to make sure all monopipes (vents, scrubbers, etc...) are connected to pipes
		/// </summary>
		[Test]
		public void MonoPipeConnectedToNet()
		{
			var monoPipes = RootObjects.ComponentsInChildren<MonoPipe>().NotNull().ToList();

			foreach (var mono in monoPipes)
			{
				mono.pipeData.MonoPipe = mono;

				int offset = PipeFunctions.GetOffsetAngle(mono.transform.localRotation.eulerAngles.z);
				mono.pipeData.Connections.Rotate(offset);
			}

			foreach (var device in monoPipes)
			{
				var vent = device as AirVent;
				if (vent != null && vent.SelfSufficient) continue;

				var scrubber = device as Scrubber;
				if (scrubber != null && scrubber.SelfSufficient) continue;

				if (device.pipeData.SelfSufficient) continue;

				if (device.pipeData.MappingNotRequiresLink) continue;

				var pipeLayer = device.transform.parent.OrNull()?.parent.OrNull()?.GetComponentInChildren<PipeLayer>();

				if (pipeLayer == null)
				{
					Report.Fail().AppendLine($"{Scene.name}: {device.gameObject.ExpensiveName()} worldPos: {device.transform.position} localPos: {device.transform.localPosition}, cannot find pipe layer!");
					continue;
				}

				var connectionsNeeded = 0;

				foreach (var connection in device.pipeData.Connections.Directions)
				{
					if(connection.Bool == false) continue;
					if(connection.MappedNeeded == false) continue;

					connectionsNeeded++;
				}

				if(connectionsNeeded == 0) continue;

				var pipes = GetConnectedPipes(device.pipeData,
					device.transform.localPosition.RoundToInt(), pipeLayer, monoPipes);

				if (connectionsNeeded == pipes.Count)
				{
					continue;
				}

				Report.Fail()
					.AppendLine($"\n{Scene.name}: {device.name} worldPos: {device.transform.position} localPos: {device.transform.localPosition}")
					.AppendLine($"has {pipes.Count} pipe connections but needs {connectionsNeeded}!");

				foreach (var pipe in pipes)
				{
					Report.AppendLine($"{pipe.Item2}");
				}
			}

			Report.AssertPassed();
		}

		private List<(PipeData, string)> GetConnectedPipes(PipeData pipeData, Vector3Int location, PipeLayer pipeLayer, List<MonoPipe> monoPipes)
		{
			var pipes = new List<(PipeData, string)>();

			for (var i = 0; i < pipeData.Connections.Directions.Length; i++)
			{
				if (pipeData.Connections.Directions[i].Bool)
				{
					Vector3Int searchVector = Vector3Int.zero;
					switch (i)
					{
						case (int) PipeDirection.North:
							searchVector = Vector3Int.up;
							break;

						case (int) PipeDirection.East:
							searchVector = Vector3Int.right;
							break;

						case (int) PipeDirection.South:
							searchVector = Vector3Int.down;
							break;

						case (int) PipeDirection.West:
							searchVector = Vector3Int.left;
							break;
					}

					searchVector = location + searchVector;
					searchVector.z = 0;
					var pipesOnTile = GetPipes(pipeLayer, searchVector, monoPipes);
					foreach (var pipe in pipesOnTile)
					{
						if (PipeFunctions.ArePipeCompatible(pipeData, i, pipe.Item1, out var pipe1ConnectAndType))
						{
							pipe1ConnectAndType.Connected = pipe.Item1;
							pipes.Add((pipe.Item1, pipe.Item2));
						}
					}
				}
			}

			return pipes;
		}

		private List<(PipeData, string)> GetPipes(PipeLayer pipeLayer, Vector3Int localPos, List<MonoPipe> monoPipes)
		{
			var pipeData = new List<(PipeData, string)>();

			//-5 to 5 z, hopefully enough?
			var count = -5;
			var position = localPos;

			//Apparently theres no good way to get all tiles in the same x,y but different z
			while (count <= 5)
			{
				position.z = count;
				var pipe = pipeLayer.Tilemap.GetTile(position);

				var pipeTile = pipe as PipeTile;
				if (pipeTile == null)
				{
					count++;
					continue;
				}

				var pipeTilesRotation = pipeLayer.Tilemap.GetTransformMatrix(position);
				var offset = PipeFunctions.GetOffsetAngle(pipeTilesRotation.rotation.eulerAngles.z);
				var data = new PipeData();
				data.SetUp(pipeTile, offset);

				pipeData.Add((data, pipeTile.name));

				count++;
			}

			var pipeLayerParentTransform = pipeLayer.transform.parent;

			var monoStuff = monoPipes.Where(x => x.transform.localPosition.RoundToInt() == localPos &&
			                                     x.transform.parent.OrNull()?.parent == pipeLayerParentTransform);

			foreach (var mono in monoStuff)
			{
				pipeData.Add((mono.pipeData, mono.name));
			}

			return pipeData;
		}
	}
}
