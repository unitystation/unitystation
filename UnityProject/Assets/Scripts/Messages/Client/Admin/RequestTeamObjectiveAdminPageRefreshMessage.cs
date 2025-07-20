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
			if (HasPermission(TAG.MANAGE_ANTAGONISTS))
			{
				TeamObjectiveAdminPageRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.AccountId);
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