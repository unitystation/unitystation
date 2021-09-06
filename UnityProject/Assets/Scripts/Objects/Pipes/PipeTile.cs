using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.Pipes;


namespace Objects.Atmospherics
{
	[CreateAssetMenu(fileName = "PipeTile_Tile", menuName = "Tiles/PipeTile")]
	public class PipeTile : BasicTile
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
				var existingConnection = pipeNode.pipeData.Connections;
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

		public override bool IsTileRepeated(Matrix4x4 thisTransformMatrix,BasicTile basicTile, Matrix4x4 TransformMatrix, MetaDataNode metaDataNode)
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

		public void InitialiseNode(Vector3Int Location, Matrix matrix)
		{
			var ZeroedLocation = new Vector3Int(x: Location.x, y: Location.y, 0);
			var metaData = matrix.MetaDataLayer.Get(ZeroedLocation);
			var pipeNode = new PipeNode();
			var rotation = matrix.UnderFloorLayer.Tilemap.GetTransformMatrix(Location);
			int Offset = PipeFunctions.GetOffsetAngle(rotation.rotation.eulerAngles.z);
			pipeNode.Initialise(this, metaData, Location, matrix, Offset);
			metaData.PipeData.Add(pipeNode);
		}
	}
}
