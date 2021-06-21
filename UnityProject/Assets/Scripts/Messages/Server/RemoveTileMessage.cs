using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Messages.Server
{
	public class RemoveTileMessage : ServerMessage<RemoveTileMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public Vector3 Position;
			public LayerType LayerType;
			public uint MatrixSyncNetId;
		}

		public static List<delayedData> DelayedStuff = new List<delayedData>();

		public struct delayedData
		{
			public Vector3 Position;
			public LayerType LayerType;
			public uint MatrixSyncNetId;
			public delayedData(Vector3 inPosition, LayerType inLayerType, uint inMatrixSyncNetId)
			{
				Position = inPosition;
				LayerType = inLayerType;
				MatrixSyncNetId = inMatrixSyncNetId;
			}
		}
		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.MatrixSyncNetId);
			if (NetworkObject == null)
			{
				DelayedStuff.Add(new delayedData(msg.Position, msg.LayerType, msg.MatrixSyncNetId));
			}
			else
			{
				var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
				tileChangerManager.InternalRemoveTile(msg.Position, msg.LayerType);
				TryDoNotDoneTiles();
			}
		}

		public void TryDoNotDoneTiles()
		{
			for (int i = 0; i < DelayedStuff.Count; i++)
			{
				NetworkObject = null;
				LoadNetworkObject(DelayedStuff[i].MatrixSyncNetId);
				if (NetworkObject != null)
				{
					var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
					tileChangerManager.InternalRemoveTile(DelayedStuff[i].Position, DelayedStuff[i].LayerType);
					DelayedStuff.RemoveAt(i);
					i--;
				}
			}
		}

		public static NetMessage Send(uint matrixSyncNetId, Vector3 position, LayerType layerType)
		{
			NetMessage msg = new NetMessage
			{
				Position = position,
				LayerType = layerType,
				MatrixSyncNetId = matrixSyncNetId
			};

			SendToAll(msg);
			return msg;
		}
	}
}