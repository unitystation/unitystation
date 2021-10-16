using System.Collections.Generic;
using System.Linq;
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

		public static List<DelayedData> DelayedStuff = new List<DelayedData>();

		public struct DelayedData
		{
			public Vector3Int Position;
			public TileType TileType;
			public LayerType layerType;
			public string TileName;
			public uint MatrixSyncNetID;

			public Matrix4x4 TransformMatrix;
			public Color Colour;

			public DelayedData(Vector3Int inPosition, TileType inTileType, string inTileName,
				Matrix4x4 inTransformMatrix,
				Color inColour, uint inMatrixSyncNetID, LayerType inlayerType)
			{
				Position = inPosition;
				TileType = inTileType;
				TileName = inTileName;
				TransformMatrix = inTransformMatrix;
				Colour = inColour;
				MatrixSyncNetID = inMatrixSyncNetID;
				layerType = inlayerType;
			}

			public DelayedData(TileChangeEntry TileChangeEntry, uint inMatrixSyncNetID )
			{
				Position = TileChangeEntry.Position;
				TileType = TileChangeEntry.TileType;
				TileName = TileChangeEntry.TileName;
				TransformMatrix = TileChangeEntry.transformMatrix.GetValueOrDefault(Matrix4x4.identity);
				Colour = TileChangeEntry.color.GetValueOrDefault(Vector4.one);
				MatrixSyncNetID = inMatrixSyncNetID;
				layerType = TileChangeEntry.LayerType;
			}
		}


		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer) return;
			LoadNetworkObject(msg.MatrixSyncNetID);

			if (NetworkObject == null)
			{
				DelayedStuff.AddRange(msg.Changes);
			}
			else
			{
				var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
				foreach (var Change in msg.Changes)
				{
					if (Change.TileType == TileType.None)
					{
						tileChangerManager.InternalRemoveTile(Change.Position, Change.layerType);
					}
					else
					{
						tileChangerManager.InternalUpdateTile(Change.Position, Change.TileType, Change.TileName, Change.TransformMatrix,
							Change.Colour);
					}
				}

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


		public static void SendTo(GameObject managerSubject, NetworkConnection recipient, TileChangeList changeList)
		{
			if (changeList == null || changeList.List.Count == 0) return;
			var netID = managerSubject.GetComponent<NetworkedMatrix>().MatrixSync.netId;
			foreach (var changeChunk in changeList.List.ToArray().Chunk(MAX_CHANGES_PER_MESSAGE))
			{
				// foreach (var entry in changeChunk.List)
				// {
				// 	Logger.LogTraceFormat("Sending update for {0} layer {1}", Category.TileMaps, entry.Position,
				// 		entry.LayerType);
				// }
				//  I imagine that doesn't help performance /\
				List<DelayedData> Changes = new List<DelayedData>();

				foreach (var tileChangeEntry in changeChunk)
				{
					Changes.Add(new DelayedData(tileChangeEntry, netID));
				}

				NetMessage msg = new NetMessage
				{
					MatrixSyncNetID = netID,
					Changes = Changes
				};

				SendTo(recipient, msg);
			}
		}

		public static NetMessage Send(uint matrixSyncNetID, Vector3Int position, TileType tileType,
			string tileName,
			Matrix4x4 transformMatrix, Color colour, LayerType LayerType)
		{
			NetMessage msg = new NetMessage
			{
				Changes = new List<DelayedData>()
				{
					new DelayedData(position,tileType,tileName,transformMatrix, colour, matrixSyncNetID, LayerType)
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
					MatrixSyncNetID = message.MatrixSyncNetID,
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