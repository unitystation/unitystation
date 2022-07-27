using System.Linq;
using Mirror;

namespace Messages.Server
{
	public class InfectedMessage : ServerMessage<InfectedMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint netId;
			public short SpriteIndex;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.netId);
			if(NetworkObject == null) return;
			if(NetworkObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return;

			playerScript.playerSprites.InfectedSpriteHandler.ChangeSprite(msg.SpriteIndex, false);
		}

		public static void Send(PlayerScript infectedPlayer, short spriteIndex)
		{
			//Send to aliens and ghosts
			var players = PlayerList.Instance.InGamePlayers.Where(x =>
				x.Script.PlayerType is PlayerTypes.Alien or PlayerTypes.Ghost);

			var msg = new NetMessage
			{
				netId = infectedPlayer.netId,
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
				netId = infectedPlayer.netId,
				SpriteIndex = spriteIndex
			};

			SendTo(conn, msg);
		}
	}
}