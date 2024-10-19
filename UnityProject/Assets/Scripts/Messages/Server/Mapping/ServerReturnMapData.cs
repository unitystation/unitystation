using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages.Server;
using Messages.Server.AdminTools;
using Mirror;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class ServerReturnMapData : ServerMessage<ServerReturnMapData.NetMessage>
{
	public static int SevenAvailableID = 0;

	public static Dictionary<int, List<string>> SaveDatas = new Dictionary<int, List<string>>();

	public struct NetMessage : NetworkMessage
	{
		public int ID;
		public string Data;
		public bool end;
		public MessageType MessageType;
		public bool DoStraightaway;
		public int MatrixID;
	}

	public enum MessageType
	{
		MapDataFromSave,
		MapDataForClient
	}

	public override void Process(NetMessage msg)
	{

		if (SaveDatas.ContainsKey(msg.ID) == false)
		{
			SaveDatas[msg.ID] = new List<string>();
		}

		SaveDatas[msg.ID].Add(msg.Data);

		if (msg.end) //NOTE presume they come in order?
		{
			var data = String.Join("", SaveDatas[msg.ID]);
			SaveDatas.Remove(msg.ID);
			switch (msg.MessageType)
			{
				case (MessageType.MapDataFromSave):
					CopyAndPaste.Instance.ReceiveData(data);
					break;

				case (MessageType.MapDataForClient):
					CustomNetworkManager.Instance.ReceiveMattOverrides(
						JsonConvert.DeserializeObject<MapSaver.MapSaver.CompactObjectMapData>(data), msg.DoStraightaway, msg.MatrixID);
					break;
			}
		}
	}

	public static List<string> ChunkString(string str, int chunkSize)
	{
		List<string> chunks = new List<string>();
		for (int i = 0; i < str.Length; i += chunkSize)
		{
			if (i + chunkSize > str.Length)
			{
				chunks.Add(str.Substring(i));
			}
			else
			{
				chunks.Add(str.Substring(i, chunkSize));
			}
		}
		return chunks;
	}

	public static void Send(GameObject recipient, string data, MessageType Type, int MatrixID)
	{
		 var stringChunks = ChunkString(data,5000);
		 int id = GetNextAvailableID();

		 int chunkCount = stringChunks.Count;
		 for (int i = 0; i < chunkCount; i++)
		 {
			 var chunk = stringChunks[i];
			 NetMessage msg = new NetMessage
			 {
				 ID = id,
				 Data = chunk,
				 end = (i + 1) == chunkCount,
				 MessageType = Type,
				 DoStraightaway = true,
				 MatrixID = MatrixID
			 };

			 SendTo(recipient, msg);
		 }

	}

	private static int nextAvailableID = 0;

	private static int GetNextAvailableID()
	{
		return nextAvailableID++;
	}

	public static void SendAll(string data, MessageType Type, bool DoStraightaway, int MatrixID)
	{
		var stringChunks = ChunkString(data,5000);

		int ID = SevenAvailableID;
		SevenAvailableID++;


		for (int i = 0; i < stringChunks.Count; i++)
		{
			NetMessage msg = new NetMessage
			{
				ID = ID,
				Data = new string(stringChunks[i].ToArray()),
				end = (i + 1) >= stringChunks.Count,
				MessageType = Type,
				DoStraightaway = DoStraightaway,
				MatrixID =  MatrixID
			};

			SendToAll(msg);
		}

	}
}
