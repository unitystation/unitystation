using Mirror;
using US13.Core.Addressables.Types;
using US13.Core.Cooldowns;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Systems.Ai;

namespace US13.Messages.Client
{
	public class RequestVOXsay : ClientMessage<RequestVOXsay.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string VoxMessage;

		}

		public override void Process(NetMessage msg)
		{
			if (SentByPlayer == PlayerInfo.Invalid) return;
			if (Cooldowns.TryStartServer(SentByPlayer.Script, CommonCooldowns.Instance.Interaction, 0.25f) == false) return;
			var Player = SentByPlayer.Script.GetComponent<AiPlayer>();
			if (Player == null) return;
			if (Player.HasDied) return;

			SoundManager.PlayNetworked(new AddressableAudioSource(){AssetAddress = Player.VOXStringLine + msg.VoxMessage  + Player.VOXStringLineEnd }, new AudioSourceParameters( spatialBlend: 1));
		}

		//This is only used to send the chat input on the client to the server
		public static NetMessage Send(string VoxMessage)
		{
			NetMessage msg = new NetMessage
			{
				VoxMessage = VoxMessage
			};

			Send(msg);
			return msg;
		}
	}
}