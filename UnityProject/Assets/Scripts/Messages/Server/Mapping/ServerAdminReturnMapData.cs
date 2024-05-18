using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages.Server;
using Messages.Server.AdminTools;
using Mirror;
using UnityEditor;
using UnityEngine;

public class ServerAdminReturnMapData : ServerMessage<ServerAdminReturnMapData.NetMessage>
{
	public static int SevenAvailableID = 0;

	public static Dictionary<int, List<string>> SaveDatas = new Dictionary<int, List<string>>();

	public struct NetMessage : NetworkMessage
	{
		public int ID;
		public string Data;
		public bool end;
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
			CopyAndPaste.Instance.ReceiveData(data);
		}
	}

	public static void Send(GameObject recipient, string data)
	{
		var StringDatas = data.Chunk(5000).ToList();

		int ID = SevenAvailableID;
		SevenAvailableID++;


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

	}
}
