using System.Linq;
using Mirror;
using US13.Clothing.Eyewear;
using US13.Managers;
using US13.Player;
using US13.Player.HUDData;
using VampireHUD = US13.Clothing.Eyewear.VampireHUD;

namespace US13.Messages.Server
{
	public class VampireHudMessage : ServerMessage<VampireHudMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool hudState;
		}

		public override void Process(NetMessage msg)
		{
			var hudType = typeof(VampireHUD);
			if (HUDHandler.Categorys.ContainsKey(hudType))
			{
				var Listy = HUDHandler.Categorys[hudType];
				foreach (var HUD in Listy)
				{
					HUD.SetVisible(msg.hudState);
				}
			}
			HUDHandler.CategoryEnabled[hudType] = msg.hudState;
		}

		public static void SendTo(NetworkConnectionToClient conn, bool _hudState)
		{
			var msg = new NetMessage
			{
				hudState = _hudState
			};
			SendTo(conn, msg);
		}
	}
}