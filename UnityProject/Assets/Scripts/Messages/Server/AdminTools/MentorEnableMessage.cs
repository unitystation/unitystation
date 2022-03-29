using Mirror;

namespace Messages.Server.AdminTools
{
	/// <summary>
	/// Allows the client to use mentor only features. A valid mentor token is required
	/// to use mentor tools.
	/// </summary>
	public class MentorEnableMessage : ServerMessage<MentorEnableMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool Enable;
			public string MentorToken;
		}

		public override void Process(NetMessage msg)
		{
			PlayerList.Instance.SetClientAsMentor(msg.MentorToken);
			UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(msg.Enable);
		}

		public static NetMessage Send(NetworkConnection player, string mentorToken, bool enable = true)
		{
			UIManager.Instance.mentorChatButtons.ServerUpdateAdminNotifications(player);
			NetMessage msg = new NetMessage
			{
				Enable = enable,
				MentorToken = mentorToken
			};

			SendTo(player, msg);
			return msg;
		}
	}
}