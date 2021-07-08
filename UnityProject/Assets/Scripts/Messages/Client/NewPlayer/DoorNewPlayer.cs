using Doors;
using Mirror;

namespace Messages.Client.NewPlayer
{
	public class DoorNewPlayer : ClientMessage<DoorNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Door;
		}

		public override void Process(NetMessage msg)
		{
			// LoadNetworkObject returns bool, so it can be used to check if object is loaded correctly
			if (LoadNetworkObject(msg.Door))
			{
				// Old doors
				if (NetworkObject.TryGetComponent(out DoorController doorController))
				{
					doorController.UpdateNewPlayer(SentByPlayer.Connection);
					return;
				}

				// New doors
				if (NetworkObject.TryGetComponent(out DoorMasterController doorMasterController))
				{
					doorMasterController.UpdateNewPlayer(SentByPlayer.Connection);
				}
			}
		}

		public static NetMessage Send(uint netId)
		{
			NetMessage msg = new NetMessage
			{
				Door = netId
			};

			Send(msg);
			return msg;
		}
	}
}
