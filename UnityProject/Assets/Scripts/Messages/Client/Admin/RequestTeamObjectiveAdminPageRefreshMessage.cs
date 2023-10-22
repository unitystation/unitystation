using Antagonists;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Client.Admin
{
	public class RequestTeamObjectiveAdminPageRefreshMessage : ClientMessage<RequestTeamObjectiveAdminPageRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{

		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				TeamObjectiveAdminPageRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.UserId);
			}
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage
			{

			};

			Send(msg);
			return msg;
		}
	}
}