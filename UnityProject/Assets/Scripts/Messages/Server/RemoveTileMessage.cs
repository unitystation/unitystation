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

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer)
			{
				return;
			}
			LoadNetworkObject(msg.MatrixSyncNetId);
			//client hasnt finished loading the scene, it'll ask for the bundle of changes aftewards
			if (NetworkObject == null)
				return;

			var tileChangerManager = NetworkObject.transform.parent.GetComponent<TileChangeManager>();
			tileChangerManager.MetaTileMap.RemoveTileWithlayer(msg.Position.RoundToInt(), msg.LayerType);
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