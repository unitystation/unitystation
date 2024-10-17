using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using Systems.Pipes;
using TileManagement;
using Tiles;

namespace Objects.Atmospherics
{
	[CreateAssetMenu(fileName = "PipeTile_Tile", menuName = "Tiles/PipeTile")]
	public class PipeTile : FuncPlaceRemoveTile
	{
		//Remember this is all static
		public PipeLayer PipeLayer = PipeLayer.Second;

		public float Volume = 0.1f;

		public CustomLogic CustomLogic;
		public Connections Connections = new Connections();
		public bool NetCompatible = true;
		public Sprite sprite;
		public override Sprite PreviewSprite => sprite;

		public static Connections GetRotatedConnection(PipeTile pipeTile, Matrix4x4 matrixStruct)
		{
			var offset = PipeFunctions.GetOffsetAngle(matrixStruct.rotation.eulerAngles.z);
			var connection = pipeTile.Connections.Copy();
			connection.Rotate(offset);
			return connection;
		}

		public static bool CanAddPipe(MetaDataNode metaData, Connections incomingConnection)
		{
			foreach (var pipeNode in metaData.PipeData)
			{
				var existingConnection = pipeNode.pipeData.RotatedConnections;
				for (var i = 0; i < incomingConnection.Directions.Length; i++)
				{
					if (incomingConnection.Directions[i].Bool && existingConnection.Directions[i].Bool)
					{
						return false;
					}
				}
			}

			return true;
		}

		public override bool IsTileRepeated(Matrix4x4 thisTransformMatrix, BasicTile basicTile,
			Matrix4x4 TransformMatrix, MetaDataNode metaDataNode)
		{
			var incomingConnection = GetRotatedConnection(this, thisTransformMatrix);
			if (CanAddPipe(metaDataNode, incomingConnection) == false)
			{
				return true;
			}

			return false;
		}

		public void InitialiseNodeNew(Vector3Int Location, Matrix matrix, Matrix4x4 Matrix4x4)
		{
			var ZeroedLocation = new Vector3Int(x: Location.x, y: Location.y, 0);
			var metaData = matrix.MetaDataLayer.Get(ZeroedLocation);
			var pipeNode = new PipeNode();
			var rotation = Matrix4x4;
			int Offset = PipeFunctions.GetOffsetAngle(rotation.rotation.eulerAngles.z);
			pipeNode.Initialise(this, metaData, Location, matrix, Offset);
			metaData.PipeData.Add(pipeNode);
		}

		public void InitialiseNode(Vector3Int Location, Matrix matrix, Matrix4x4 Matrix4x4)
		{
			var ZeroedLocation = new Vector3Int(x: Location.x, y: Location.y, 0);
			var metaData = matrix.MetaDataLayer.Get(ZeroedLocation);
			var pipeNode = new PipeNode();
			var rotation = Matrix4x4;
			int Offset = PipeFunctions.GetOffsetAngle(rotation.rotation.eulerAngles.z);
			pipeNode.Initialise(this, metaData, Location, matrix, Offset);
			metaData.PipeData.Add(pipeNode);
		}


		public override void OnPlaced(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation)
		{
			bool[] PipeDirCheck = new bool[4]; //TODO Maybe Optimise this?
			var matrixStruct = tileLocation.transformMatrix;
			var connection = PipeTile.GetRotatedConnection(this, matrixStruct);
			var pipeDir = connection.Directions;
			var canInitializePipe = true;
			for (var d = 0; d < pipeDir.Length; d++)
			{
				if (pipeDir[d].Bool == false) continue;

				if (PipeDirCheck[d])
				{
					canInitializePipe = false;
					Loggy.LogWarning(
						$"A pipe is overlapping its connection at ({TileLocation.x}, {TileLocation.y}) in {AssociatedMatrix.gameObject.scene.name} - {LayerType.ToString()} with another pipe, removing one",
						Category.Pipes);
					AssociatedMatrix.MetaTileMap.Layers[LayerType].Tilemap.SetTile(TileLocation, null);
					break;
				}

				PipeDirCheck[d] = true;
			}

			if (canInitializePipe)
			{
				this.InitialiseNode(TileLocation, AssociatedMatrix, tileLocation.transformMatrix);
			}
		}

		public override void OnRemoved(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation, bool SpawnItems)
		{
			var metaDataNode = AssociatedMatrix.GetMetaDataNode(TileLocation);
			for (var i = 0; i < metaDataNode.PipeData.Count; i++)
			{
				if (metaDataNode.PipeData[i].RelatedTile != this)
					continue; //TODO Stuff like layers and stuff can be included

				metaDataNode.PipeData[i].pipeData.DestroyThis(true, tileLocation.transformMatrix, tileLocation.Colour, SpawnItems);
			}
		}
	}
}