using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

public class MetaDataLayerMessage : ServerMessage<MetaDataLayerMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public List<DelayedData> Changes;
		public uint MatrixSyncNetID;
	}

	//just a best guess, try increasing it until the message exceeds mirror's limit
	private static readonly int MAX_CHANGES_PER_MESSAGE = 350;


	public override void Process(NetMessage msg)
	{
		if (CustomNetworkManager.IsServer) return;
		LoadNetworkObject(msg.MatrixSyncNetID);

		var MetaDataLayer = NetworkObject.transform.parent.GetComponentInChildren<MetaDataLayer>();
		foreach (var Change in msg.Changes)
		{
			MetaDataLayer.Get(Change.Position).IsSlippery = Change.IsSlippy;
		}
	}

	public static void SendTo(GameObject managerSubject, NetworkConnection recipient,
		Dictionary<Vector3Int, MetaDataNode> changeList)
	{
		if (changeList == null || changeList.Count == 0) return;
		var netID = managerSubject.transform.parent.GetComponent<NetworkedMatrix>().MatrixSync.netId;
		foreach (var changeChunk in changeList.Chunk(MAX_CHANGES_PER_MESSAGE))
		{
			List<DelayedData> Changes = new List<DelayedData>();

			foreach (var metaData in changeChunk)
			{
				Changes.Add(new DelayedData()
				{
					Position = metaData.Key,
					IsSlippy = metaData.Value.IsSlippery
				});
			}

			NetMessage msg = new NetMessage
			{
				MatrixSyncNetID = netID,
				Changes = Changes
			};

			SendTo(recipient, msg);
		}
	}

	public static void Send(GameObject managerSubject, List<MetaDataNode> changeList)
	{
		var netID = managerSubject.transform.parent.GetComponent<NetworkedMatrix>().MatrixSync.netId;
		// foreach (var changeChunk in changeList.Chunk(MAX_CHANGES_PER_MESSAGE)) //TODO Check it's not too big maybe
		// {
		List<DelayedData> Changes = new List<DelayedData>();

		foreach (var metaData in changeList)
		{
			Changes.Add(new DelayedData()
			{
				Position = metaData.Position,
				IsSlippy = metaData.IsSlippery
			});
		}

		NetMessage msg = new NetMessage
		{
			MatrixSyncNetID = netID,
			Changes = Changes
		};
		SendToAll(msg);
		// }
	}

	public struct DelayedData
	{
		public Vector3Int Position;
		public bool IsSlippy;
	}
}

public static class UpdateTileMessageReaderWriters
{
	public static MetaDataLayerMessage.NetMessage Deserialize(this NetworkReader reader)
	{
		var message = new MetaDataLayerMessage.NetMessage();
		message.Changes = new List<MetaDataLayerMessage.DelayedData>();
		message.MatrixSyncNetID = reader.ReadUInt();
		while (true)
		{
			var Continue = reader.ReadBool();
			if (Continue == false)
			{
				break;
			}

			var WorkingOn = new MetaDataLayerMessage.DelayedData
			{
				Position = reader.ReadVector3Int(),
				IsSlippy = reader.ReadBool()
			};

			message.Changes.Add(WorkingOn);
		}

		return message;
	}

	public static void Serialize(this NetworkWriter writer, MetaDataLayerMessage.NetMessage message)
	{
		writer.WriteUInt(message.MatrixSyncNetID);
		foreach (var delayedData in message.Changes)
		{
			writer.WriteBool(true);
			writer.WriteVector3Int(delayedData.Position);
			writer.WriteBool(delayedData.IsSlippy);
		}

		writer.WriteBool(false);
	}
}