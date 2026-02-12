using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using Newtonsoft.Json;
using US13.Systems.Antagonists;
using US13.UI.Systems.AdminTools.ObjectiveManager;

namespace US13.Messages.Client.Admin
{
	public class RequestAdminTeamUpdateMessage : ClientMessage<RequestAdminTeamUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string json;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.MANAGE_ANTAGONISTS) == true)
			{
				var info = JsonConvert.DeserializeObject<TeamsInfo>(msg.json);
				try
				{
					TeamObjectiveAdminPage.ProcessServer(info);
				}
				catch (Exception ex)
				{
					Loggy.Error($"[RequestAdminObjectiveUpdateMessage/Process] Failed to process teams update \n{msg.json}\n {ex}");
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