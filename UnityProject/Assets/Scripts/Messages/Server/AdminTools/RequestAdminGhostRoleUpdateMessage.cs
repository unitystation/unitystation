using AdminTools;
using Messages.Client;
using Mirror;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Messages.Server.AdminTools
{
	public class RequestAdminGhostRoleUpdateMessage : ClientMessage<RequestAdminGhostRoleUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string json;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				var information = JsonConvert.DeserializeObject<GhostRolesInfo>(msg.json);

				GhostRoleAdminPage.ProceedGhostRolesUpdate(information);
			}
		}

		public static NetMessage Send(List<GhostRoleInfo> info)
		{
			var objs = new GhostRolesInfo()
			{
				Roles = info
			};

			NetMessage msg = new NetMessage
			{
				json = JsonConvert.SerializeObject(objs)
			};

			Send(msg);
			return msg;
		}
	}
}