using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Objects.Atmospherics;
using Systems.Electricity;
using Systems.Pipes;
using UnityEngine;
using PipeLayer = Tilemaps.Behaviours.Layers.PipeLayer;

namespace Tests.Scenes
{
	public class PipeTests : SceneTest
	{
		public PipeTests(SceneTestData data) : base(data) { }

		private static Vector3Int[] directions = new[]
		{
			new Vector3Int(0, 1),
			new Vector3Int(1, 0),
			new Vector3Int(0, -1),
			new Vector3Int(-1, 0)
		};

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

				if(mono.SpawnedFromItem == false) continue;

				int offset = PipeFunctions.GetOffsetAngle(mono.transform.localRotation.eulerAngles.z);
				mono.pipeData.Connections.Rotate(offset);
			}

			foreach (var device in monoPipes)
			{
				var vent = device as AirVent;
				if (vent != null && vent.SelfSufficient) continue;

				var scrubber = device as Scrubber;
				if (scrubber != null && scrubber.SelfSufficient) continue;

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

			var monoStuff = monoPipes.Where(x => x.transform.localPosition == localPos &&
			                                     x.transform.parent.OrNull()?.parent == pipeLayerParentTransform);

			foreach (var mono in monoStuff)
			{
				pipeData.Add((mono.pipeData, mono.name));
			}

			return pipeData;
		}
	}
}