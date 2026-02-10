using Mirror;
using US13.Managers;

namespace US13.Messages.Client.Admin
{
	public class AdminRequestKickPlayerFromMind : ClientMessage<AdminRequestKickPlayerFromMind.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint MindID;
		}

		public override void Process(NetMessage msg)
		{

			if (HasPermission(TAG.MANAGE_MIND_OWNERSHIP)) //TODO tagss!!
			{
				var Mind = MindManager.Instance.minds[msg.MindID];

				if (Mind.ControlledBy != null)
				{
					Mind.ControlledBy.GenNewMind();
				}
			}

		}

		public static NetMessage Send(uint MindID)
		{
			var msg = new NetMessage
			{
				MindID = MindID
			};

			Send(msg);
			return msg;
		}
	}
}
