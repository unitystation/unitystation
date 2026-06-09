using Mirror;
using US13.Managers;
using US13.Managers.NetworkManagement;

namespace US13.Messages.Client.Admin
{
	public class AdminRequestSetMindPossessing : ClientMessage<AdminRequestSetMindPossessing.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint MindID;
			public uint ObjectID;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.MANAGE_MIND_POSSESSING)) //TODO tagss!!
			{
				var Mind = MindManager.Instance.minds[msg.MindID];
				var Object = CustomNetworkManager.Spawned[msg.ObjectID];
				Mind.SetPossessingObject(Object.gameObject);
			}
		}

		public static NetMessage Send(uint MindID, uint ObjectID )
		{
			var msg = new NetMessage
			{
				MindID = MindID,
				ObjectID = ObjectID
			};

			Send(msg);
			return msg;
		}
	}
}

