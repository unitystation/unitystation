using System.Linq;
using Mirror;
using Player;

namespace Messages.Server
{
	public class InfectedMessage : ServerMessage<InfectedMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint NetId;
			public short SpriteIndex;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.NetId);
			if(NetworkObject == null) return;
			if(NetworkObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return;
			playerScript.playerSprites.InfectedSpriteHandler.PushTexture();
			playerScript.playerSprites.InfectedSpriteHandler.ChangeSprite(msg.SpriteIndex, false);
		}

		public static void Send(PlayerScript infectedPlayer, short spriteIndex)
		{
			//Send to all who have AlienInfectionViewer
			var players = PlayerList.Instance.InGamePlayers.Where(x =>
				x.Script.GetComponent<AlienInfectionViewer>() != null);

			var msg = new NetMessage
			{
				NetId = infectedPlayer.netId,
				SpriteIndex = spriteIndex
			};

			foreach (var player in players)
			{
				SendTo(player, msg);
			}
		}

		public static void SendTo(NetworkConnectionToClient conn, PlayerScript infectedPlayer, short spriteIndex)
		{
			var msg = new NetMessage
			{
				NetId = infectedPlayer.netId,
				SpriteIndex = spriteIndex
			};

			SendTo(conn, msg);
		}
	}
}