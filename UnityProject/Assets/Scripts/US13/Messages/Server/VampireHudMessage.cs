using System.Linq;
using Mirror;
using US13.Clothing.Eyewear;
using US13.Managers;
using US13.Player;
using US13.Player.HUDData;

namespace US13.Messages.Server
{
	public class VampireHudMessage : ServerMessage<VampireHudMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint netId;
			public bool hudState;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.netId);
			if(NetworkObject == null) return;
			if(NetworkObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return;
			if(playerScript.TryGetComponent<VampireHUD>(out var vampireHud) == false) return;

			var hudType = typeof(MedicalHUD);
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

		public static void SendTo(NetworkConnectionToClient conn, PlayerScript vampirePlayer, bool _hudState)
		{
			var msg = new NetMessage
			{
				netId = vampirePlayer.netId,
				hudState = _hudState
			};
			SendTo(conn, msg);
		}
	}
}