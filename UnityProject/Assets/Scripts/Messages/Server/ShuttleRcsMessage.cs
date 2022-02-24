using Mirror;
using Objects.Shuttles;
namespace Messages.Server
{
	public class ShuttleRcsMessage : ServerMessage<ShuttleRcsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint shuttleConsoleUINT;
			public bool State;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.shuttleConsoleUINT);
			var shuttleConsole = NetworkObject.GetComponent<ShuttleConsole>();
			shuttleConsole.ChangeRcsPlayer(msg.State, PlayerManager.LocalPlayerScript);
		}

		public static NetMessage SendTo(ShuttleConsole shuttleConsole, bool state, ConnectedPlayer connectedPlayer)
		{
			NetMessage msg = new NetMessage
			{
				shuttleConsoleUINT = shuttleConsole.netId,
				State = state
			};

			SendTo(connectedPlayer, msg);
			return msg;
		}
	}
}