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
				// https://docs.unity3d.com/2019.3/Documentation/ScriptReference/Component.TryGetComponent.html
				if (NetworkObject.TryGetComponent(out DoorController doorController))
				{
					doorController.UpdateNewPlayer(SentByPlayer.Connection);
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
