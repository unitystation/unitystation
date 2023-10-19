﻿using AdminTools;
using Logs;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
namespace Messages.Client.Admin
{
	public class RequestAdminObjectiveUpdateMessage : ClientMessage<RequestAdminObjectiveUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string playerForRequestID;
			public string json;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				var info = JsonConvert.DeserializeObject<AntagonistInfo>(msg.json);
				try
				{
					var player = PlayerList.Instance.GetPlayerByID(msg.playerForRequestID);

					PlayerObjectiveManagerPage.ProceedServerObjectivesUpdate(info, player.Mind);
				}
				catch (Exception ex)
				{
					Loggy.LogError($"[RequestAdminObjectiveUpdateMessage/Process] Failed to process objective update {ex}");
				}
			}
		}

		public static NetMessage Send(string playerForRequestID, AntagonistInfo objs)
		{
			NetMessage msg = new NetMessage
			{
				playerForRequestID = playerForRequestID,
				json = JsonConvert.SerializeObject(objs)
			};

			Send(msg);
			return msg;
		}
	}
}