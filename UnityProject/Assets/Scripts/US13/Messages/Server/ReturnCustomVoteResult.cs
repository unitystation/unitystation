using Mirror;
using US13.Messages.Client.Admin;
using US13.Systems.Voting;

namespace US13.Messages.Server
{
	public class ReturnCustomVoteResult : ServerMessage<ReturnCustomVoteResult.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Result;
		}

		public override void Process(NetMessage msg)
		{
			AdminVoteUI.Instance.ReceiveResult(msg.Result);
		}

		public static NetMessage Send(string Results)
		{
			var msg = new NetMessage
			{
				Result = Results
			};
			SendToAdmins(msg, TAG.ADMIN_VOTE_VETO);
			return msg;
		}
	}
}
