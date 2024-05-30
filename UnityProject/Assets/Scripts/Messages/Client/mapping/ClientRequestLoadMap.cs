using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapSaver;
using Messages.Client;
using Mirror;
using Newtonsoft.Json;
using TileManagement;
using UnityEngine;

public class ClientRequestLoadMap : ClientMessage<ClientRequestLoadMap.NetMessage>
{
	public static Dictionary<PlayerInfo, List<string>> SaveDatas = new Dictionary<PlayerInfo, List<string>>();

	public struct NetMessage : NetworkMessage
	{
		public Vector3 Offset00; //the off set In the data so objects will appear at 0,0
		public Vector3 Offset; //Offset to apply 0,0 to get the position you want
		public string Data;
		public bool end;
		public int MatrixID;
		public LayerType[] LoadLayers;
		public bool LoadObjects;
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin() == false) return;

		if (SaveDatas.ContainsKey(SentByPlayer) == false)
		{
			SaveDatas[SentByPlayer] = new List<string>();
		}

		SaveDatas[SentByPlayer].Add(msg.Data);

		if (msg.end) //NOTE presume they come in order?
		{
			var data = String.Join("", SaveDatas[SentByPlayer]);
			SaveDatas.Remove(SentByPlayer);
			var mapdata = JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(data);
			HashSet<LayerType> LoadLayers = null;
			if (msg.LoadLayers != null)
			{
				LoadLayers = msg.LoadLayers.ToHashSet();
			}

			MapLoader.LoadSection( MatrixManager.Get(msg.MatrixID),   msg.Offset00, msg.Offset, mapdata, LoadLayers, msg.LoadObjects);

			if (msg.LoadObjects)
			{
				var newdata = JsonConvert.SerializeObject(mapdata.CompactObjectMapData);
				CustomNetworkManager.LoadedMapDatas.Add(newdata);
				ServerReturnMapData.SendAll( newdata , ServerReturnMapData.MessageType.MapDataForClient, true);
			}
		}

	}

	public static void Send(string data, Matrix Matrix,  Vector3 Offset00,Vector3 Offset, HashSet<LayerType> Layers = null, bool LoadObjects = true)
	{

		var StringDatas = data.Chunk(5000).ToList();
		for (int i = 0; i < StringDatas.Count; i++)
		{
			NetMessage  msg = new NetMessage
			{
				MatrixID =  Matrix.Id,
				Data = new string(StringDatas[i].ToArray()),
				end = (i + 1) >= StringDatas.Count,
				Offset = Offset,
				Offset00 = Offset00,
				LoadLayers = Layers?.ToArray(),
				LoadObjects = LoadObjects
			};

			Send(msg);
		}

	}
}
