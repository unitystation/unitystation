using Messages.Server;
using Mirror;
using UnityEngine;

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
