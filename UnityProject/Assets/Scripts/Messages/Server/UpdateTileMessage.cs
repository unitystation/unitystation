using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TileManagement;
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
				Position = TileChangeEntry.position;
				layerType = TileChangeEntry.LayerType;
				if (TileChangeEntry.RelatedTileLocation?.layerTile == null)
				{
					TileType = TileType.None;
					Colour = Color.white;
					TransformMatrix = Matrix4x4.identity;
					TileName = "";
				}
				else
				{
					TileName = TileChangeEntry.RelatedTileLocation.layerTile.name;
					TileType = TileChangeEntry.RelatedTileLocation.layerTile.TileType;
					TransformMatrix = TileChangeEntry.RelatedTileLocation.transformMatrix;
					Colour = TileChangeEntry.RelatedTileLocation.Colour;
				}

			}
		}


		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer) return;
			LoadNetworkObject(msg.MatrixSyncNetID);

			//client hasnt finished loading the scene, it'll ask for the bundle of changes aftewards
			if (NetworkObject == null)
				return;

			var tileChangerManager = NetworkObject.transform.parent.GetComponentInChildren<MetaTileMap>();
			foreach (var Change in msg.Changes)
			{
				if (Change.TileType == TileType.None)
				{
					tileChangerManager.RemoveTileWithlayer(Change.Position, Change.layerType);
				}
				else
				{
					tileChangerManager.SetTile(Change.Position, Change.TileType, Change.TileName, Change.TransformMatrix,
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

	public static class UpdateTileMessageReaderWriters
	{

		public enum EnumOperation
		{
			Colour = 1,
			Matrix4x4 = 2,
			NoMoreData = 255,
		}

		public static UpdateTileMessage.NetMessage Deserialize(this NetworkReader reader)
		{
			var message = new UpdateTileMessage.NetMessage();
			message.Changes = new List<UpdateTileMessage.DelayedData>();
			message.MatrixSyncNetID = reader.ReadUInt();
			while (true)
			{
				var Continue = reader.ReadBool();
				if (Continue == false)
				{
					break;
				}

				var WorkingOn = new UpdateTileMessage.DelayedData
				{
					Position = reader.ReadVector3Int(),
					TileType = (TileType) reader.ReadInt(),
					layerType = (LayerType) reader.ReadInt(),
					TileName = reader.ReadString(),
					TransformMatrix = Matrix4x4.identity,
					Colour = Color.white
				};

				while (true)
				{
					byte Operation = reader.ReadByte();

					if (Operation == (byte)EnumOperation.NoMoreData)
					{
						break;
					}

					if (Operation == (byte)EnumOperation.Colour)
					{
						WorkingOn.Colour = reader.ReadColor();
					}

					if (Operation == (byte)EnumOperation.Matrix4x4)
					{
						WorkingOn.TransformMatrix = reader.ReadMatrix4x4();
					}
				}
				message.Changes.Add(WorkingOn);
			}

			return message;
		}

		public static void Serialize(this NetworkWriter writer, UpdateTileMessage.NetMessage message)
		{
			writer.WriteUInt(message.MatrixSyncNetID);
			foreach (var delayedData in message.Changes)
			{
				writer.WriteBool(true);
				writer.WriteVector3Int(delayedData.Position);
				writer.WriteInt((int)delayedData.TileType);
				writer.WriteInt((int)delayedData.layerType);
				writer.WriteString(delayedData.TileName);

				if (delayedData.Colour != Color.white)
				{
					writer.WriteByte((byte) EnumOperation.Colour);
					writer.WriteColor(delayedData.Colour);
				}

				if (delayedData.TransformMatrix != Matrix4x4.identity)
				{
					writer.WriteByte((byte) EnumOperation.Matrix4x4);
					writer.WriteMatrix4x4(delayedData.TransformMatrix);
				}
				writer.WriteByte((byte) EnumOperation.NoMoreData);
			}
			writer.WriteBool(false);
		}
	}
}