using Mirror;
using US13.Managers;
using US13.Objects.Consoles;
using US13.Player;

namespace US13.Messages.Server
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

		public static NetMessage SendTo(ShuttleConsole shuttleConsole, bool state, PlayerInfo connectedPlayer)
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