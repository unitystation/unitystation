using System.Linq;
using Messages.Client;
using Mirror;
using UnityEngine;

public class AdminRequestPoll : ClientMessage<AdminRequestPoll.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string Title;
		public string[] Option;

		public bool end;
	}

	public static void Send(string Title, string[] Option, bool End)
	{
		NetMessage msg = new NetMessage
		{
			Title = Title,
			Option = Option,
			end = End
		};
		Send(msg);

	}

	public override void Process(NetMessage msg)
	{
		if (HasPermission(TAG.ADMIN_VOTE_VETO) == false) return;

		if (msg.end)
		{
			VotingManager.Instance.EndVote();
		}
		else
		{
			VotingManager.Instance.SetupArbitraryVote(VotingManager.VotePolicy.MajorityRules, 30, msg.Title,
				msg.Option.ToList());
		}


	}

}
