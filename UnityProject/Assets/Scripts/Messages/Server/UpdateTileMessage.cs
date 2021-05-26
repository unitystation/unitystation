using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Messages.Server
{
	public class UpdateTileMessage : ServerMessage<UpdateTileMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public Vector3Int Position;
			public TileType TileType;
			public string TileName;
			public Matrix4x4 TransformMatrix;
			public Color Colour;
			public uint MatrixSyncNetID;
		}

		public static List<delayedData> DelayedStuff = new List<delayedData>();

		public struct delayedData
		{
			public Vector3Int Position;
			public TileType TileType;
			public string TileName;
			public Matrix4x4 TransformMatrix;
			public Color Colour;
			public uint MatrixSyncNetID;

			public delayedData(Vector3Int inPosition, TileType inTileType, string inTileName, Matrix4x4 inTransformMatrix,
				Color inColour, uint inMatrixSyncNetID)
			{
				Position = inPosition;
				TileType = inTileType;
				TileName = inTileName;
				TransformMatrix = inTransformMatrix;
				Colour = inColour;
				MatrixSyncNetID = inMatrixSyncNetID;
			}
		}


		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.MatrixSyncNetID);
			if (NetworkObject == null)
			{
				DelayedStuff.Add(new delayedData(msg.Position, msg.TileType, msg.TileName, msg.TransformMatrix, msg.Colour, msg.MatrixSyncNetID));
			}
			else
			{
				var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
				tileChangerManager.InternalUpdateTile(msg.Position, msg.TileType, msg.TileName, msg.TransformMatrix, msg.Colour);
				TryDoNotDoneTiles();
			}
		}

		public void TryDoNotDoneTiles()
		{
			for (int i = 0; i < DelayedStuff.Count; i++)
			{
				NetworkObject = null;
				LoadNetworkObject(DelayedStuff[i].MatrixSyncNetID);
				if (NetworkObject != null)
				{
					var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
					tileChangerManager.InternalUpdateTile(DelayedStuff[i].Position, DelayedStuff[i].TileType,
						DelayedStuff[i].TileName, DelayedStuff[i].TransformMatrix, DelayedStuff[i].Colour);
					DelayedStuff.RemoveAt(i);
					i--;
				}
			}
		}

		public static NetMessage Send(uint matrixSyncNetID, Vector3Int position, TileType tileType,
			string tileName,
			Matrix4x4 transformMatrix, Color colour)
		{
			NetMessage msg = new NetMessage
			{
				Position = position,
				TileType = tileType,
				TileName = tileName,
				TransformMatrix = transformMatrix,
				Colour = colour,
				MatrixSyncNetID = matrixSyncNetID
			};

			SendToAll(msg);
			return msg;
		}
	}
}