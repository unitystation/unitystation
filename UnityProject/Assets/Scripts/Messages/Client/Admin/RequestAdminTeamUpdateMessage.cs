using Logs;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Messages.Client.Admin
{
	public class RequestAdminTeamUpdateMessage : ClientMessage<RequestAdminTeamUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string json;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				var info = JsonConvert.DeserializeObject<TeamsInfo>(msg.json);
				try
				{
					TeamObjectiveAdminPage.ProcessServer(info);
				}
				catch (Exception ex)
				{
					Loggy.LogError($"[RequestAdminObjectiveUpdateMessage/Process] Failed to process teams update \n{msg.json}\n {ex}");
				}
			}
		}

		public static NetMessage Send(List<TeamInfo> info)
		{
			var teams = new TeamsInfo()
			{
				TeamsInfos = info
			};

			NetMessage msg = new NetMessage
			{
				json = JsonConvert.SerializeObject(teams)
			};

			Send(msg);
			return msg;
		}
	}
}