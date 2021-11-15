using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Tilemaps.Behaviours.Layers;

namespace Messages.Server
{
	public class UpdateTileMessage : ServerMessage<UpdateTileMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public List<DelayedData> Changes;
			public uint MatrixSyncNetID;
		}

		//just a best guess, try increasing it until the message exceeds mirror's limit
		private static readonly int MAX_CHANGES_PER_MESSAGE = 350;

		public struct DelayedData
		{
			public Vector3Int Position;
			public TileType TileType;
			public LayerType layerType;
			public string TileName;

			public Matrix4x4 TransformMatrix;
			public Color Colour;

			public DelayedData(Vector3Int inPosition, TileType inTileType, string inTileName,
				Matrix4x4 inTransformMatrix, Color inColour, LayerType inlayerType)
			{
				Position = inPosition;
				TileType = inTileType;
				TileName = inTileName;
				TransformMatrix = inTransformMatrix;
				Colour = inColour;
				layerType = inlayerType;
			}

			public DelayedData(TileChangeEntry TileChangeEntry)
			{
				Position = TileChangeEntry.Position;
				TileType = TileChangeEntry.TileType;
				TileName = TileChangeEntry.TileName;
				TransformMatrix = TileChangeEntry.transformMatrix.GetValueOrDefault(Matrix4x4.identity);
				Colour = TileChangeEntry.color.GetValueOrDefault(Vector4.one);
				layerType = TileChangeEntry.LayerType;
			}
		}


		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer) return;
			LoadNetworkObject(msg.MatrixSyncNetID);

			//client hasnt finished loading the scene, it'll ask for the bundle of changes aftewards
			if (NetworkObject == null)
				return;

			var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
			foreach (var Change in msg.Changes)
			{
				if (Change.TileType == TileType.None)
				{
					tileChangerManager.RemoveTile(Change.Position, Change.layerType);
				}
				else
				{
					tileChangerManager.InternalUpdateTile(Change.Position, Change.TileType, Change.TileName, Change.TransformMatrix,
						Change.Colour);
				}
			}
		}

		public static void SendTo(GameObject managerSubject, NetworkConnection recipient, TileChangeList changeList)
		{
			if (changeList == null || changeList.List.Count == 0) return;
			var netID = managerSubject.GetComponent<NetworkedMatrix>().MatrixSync.netId;
			foreach (var changeChunk in changeList.List.ToArray().Chunk(MAX_CHANGES_PER_MESSAGE))
			{
				List<DelayedData> Changes = new List<DelayedData>();

				foreach (var tileChangeEntry in changeChunk)
				{
					Changes.Add(new DelayedData(tileChangeEntry));
				}

				NetMessage msg = new NetMessage
				{
					MatrixSyncNetID = netID,
					Changes = Changes
				};

				SendTo(recipient, msg);
			}
		}

		public static NetMessage Send(uint matrixSyncNetID, Vector3Int position, TileType tileType, string tileName,
			Matrix4x4 transformMatrix, Color colour, LayerType LayerType)
		{
			NetMessage msg = new NetMessage
			{
				Changes = new List<DelayedData>()
				{
					new DelayedData(position, tileType, tileName, transformMatrix, colour, LayerType)
				},
				MatrixSyncNetID = matrixSyncNetID,
			};

			SendToAll(msg);
			return msg;
		}
	}
}