using Mirror;
using UI;

namespace Messages.Server
{
	/// <summary>
	///     Message that tells client which UI action to perform
	/// </summary>
	public class UpdateUIMessage : ServerMessage<UpdateUIMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ControlDisplays.Screens Screen;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Display.SetScreenFor(msg.Screen);
		}

		public static NetMessage Send(ControlDisplays.Screens screen)
		{
			NetMessage msg = new NetMessage
			{
				Screen = screen
			};

			SendToAll(msg);
			return msg;
		}
	}
}
