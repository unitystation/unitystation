using AdminTools;
using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	public class AdminPlayerAlertActions : ClientMessage<AdminPlayerAlertActions.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int ActionRequested;
			public string RoundTimeOfIncident;
			public uint PerpNetID;
			public string AdminToken;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.playerAlerts.ServerProcessActionRequest(SentByPlayer.AccountId, (PlayerAlertActions)msg.ActionRequested,
				msg.RoundTimeOfIncident, msg.PerpNetID, msg.AdminToken);
		}

		public static NetMessage Send(PlayerAlertActions actionRequested, string roundTimeOfIncident, uint perpId, string adminToken)
		{
			NetMessage msg = new NetMessage
			{
				ActionRequested = (int)actionRequested,
				RoundTimeOfIncident = roundTimeOfIncident,
				PerpNetID = perpId,
				AdminToken = adminToken
			};

			Send(msg);
			return msg;
		}
	}
}
