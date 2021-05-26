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
			public bool RemoveAll;
			public uint MatrixSyncNetId;
		}

		public static List<delayedData> DelayedStuff = new List<delayedData>();

		public struct delayedData
		{
			public Vector3 Position;
			public LayerType LayerType;
			public bool RemoveAll;
			public uint MatrixSyncNetId;
			public delayedData(Vector3 inPosition, LayerType inLayerType, bool inRemoveAll, uint inMatrixSyncNetId)
			{
				Position = inPosition;
				LayerType = inLayerType;
				RemoveAll = inRemoveAll;
				MatrixSyncNetId = inMatrixSyncNetId;
			}
		}
		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.MatrixSyncNetId);
			if (NetworkObject == null)
			{
				DelayedStuff.Add(new delayedData(msg.Position, msg.LayerType, msg.RemoveAll, msg.MatrixSyncNetId));
			}
			else
			{
				var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
				tileChangerManager.InternalRemoveTile(msg.Position, msg.LayerType, msg.RemoveAll);
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
					tileChangerManager.InternalRemoveTile(DelayedStuff[i].Position, DelayedStuff[i].LayerType, DelayedStuff[i].RemoveAll);
					DelayedStuff.RemoveAt(i);
					i--;
				}
			}
		}

		public static NetMessage Send(uint matrixSyncNetId, Vector3 position, LayerType layerType, bool removeAll)
		{
			NetMessage msg = new NetMessage
			{
				Position = position,
				LayerType = layerType,
				RemoveAll = removeAll,
				MatrixSyncNetId = matrixSyncNetId
			};

			SendToAll(msg);
			return msg;
		}
	}
}