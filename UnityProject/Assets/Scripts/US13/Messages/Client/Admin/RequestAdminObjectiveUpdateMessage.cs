using System;
using Logs;
using Mirror;
using Newtonsoft.Json;
using US13.Managers;
using US13.UI.Systems.AdminTools;
using US13.UI.Systems.AdminTools.ObjectiveManager;

namespace US13.Messages.Client.Admin
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
			if (HasPermission(TAG.MANAGE_ANTAGONISTS))
			{
				var info = JsonConvert.DeserializeObject<AntagonistInfo>(msg.json);
				try
				{
					var player = PlayerList.Instance.GetPlayerByID(msg.playerForRequestID);

					PlayerObjectiveManagerPage.ProceedServerObjectivesUpdate(info, player.Mind);
				}
				catch (Exception ex)
				{
					Loggy.Error($"[RequestAdminObjectiveUpdateMessage/Process] Failed to process objective update {ex}");
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