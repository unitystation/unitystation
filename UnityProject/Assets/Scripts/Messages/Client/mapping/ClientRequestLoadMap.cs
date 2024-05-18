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
		public string Data;
		public bool end;
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
			MapLoader.LoadSection( pos, Offset00, Offset, mapdata);
		}

	}

	public static NetMessage Send(string data)
	{

		var StringDatas = data.Chunk(5000).ToList();



		for (int i = 0; i < StringDatas.Count; i++)
		{
			NetMessage  msg = new NetMessage
			{
				ID =  ID,
				Data = new string(StringDatas[i].ToArray()),
				end = (i + 1) >= StringDatas.Count
			};

			SendTo(recipient, msg);
		}

		NetMessage msg = new NetMessage
		{
			PreviewGizmos = PreviewGizmos.ToArray(),
			Bounds = Bounds.ToArray(),
			MatrixID = Matrix.Id,
			Compact = Compact
		};

		Send(msg);
		return msg;

	}
}
