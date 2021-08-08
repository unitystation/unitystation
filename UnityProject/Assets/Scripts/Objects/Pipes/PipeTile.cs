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

		public override bool AreUnderfloorSame(Matrix4x4 thisTransformMatrix,BasicTile basicTile, Matrix4x4 TransformMatrix)
		{
			if ((basicTile as PipeTile) != null)
			{
				var TilePipeTile = (PipeTile) basicTile;
				var TheConnection = GetRotatedConnection(TilePipeTile, TransformMatrix);
				var thisConnection = GetRotatedConnection(this, thisTransformMatrix);
				for (int i = 0; i < thisConnection.Directions.Length; i++)
				{
					if (thisConnection.Directions[i].Bool && TheConnection.Directions[i].Bool)
					{
						return true;
					}
				}
				return false;
			}

			return base.AreUnderfloorSame(thisTransformMatrix, basicTile, TransformMatrix);
		}

		public void InitialiseNodeNew(Vector3Int Location, Matrix matrix, Matrix4x4 Matrix4x4)
		{
			var ZeroedLocation = new Vector3Int(x: Location.x, y: Location.y, 0);
			var metaData = matrix.MetaDataLayer.Get(ZeroedLocation);
			var pipeNode = new PipeNode();
			var rotation = Matrix4x4;
			int Offset = PipeFunctions.GetOffsetAngle(rotation.rotation.eulerAngles.z);
			pipeNode.Initialise(this, metaData, ZeroedLocation, matrix, Offset);
			metaData.PipeData.Add(pipeNode);
		}

		public void InitialiseNode(Vector3Int Location, Matrix matrix)
		{
			var ZeroedLocation = new Vector3Int(x: Location.x, y: Location.y, 0);
			var metaData = matrix.MetaDataLayer.Get(ZeroedLocation);
			var pipeNode = new PipeNode();
			var rotation = matrix.UnderFloorLayer.Tilemap.GetTransformMatrix(Location);
			int Offset = PipeFunctions.GetOffsetAngle(rotation.rotation.eulerAngles.z);
			pipeNode.Initialise(this, metaData, ZeroedLocation, matrix, Offset);
			metaData.PipeData.Add(pipeNode);
		}
	}
}
