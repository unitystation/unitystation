using AddressableReferences;
using Messages.Server.SoundMessages;
using Mirror;
using Systems.Ai;
using UnityEngine;

namespace Messages.Client
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

			SoundManager.PlayNetworked(new AddressableAudioSource(){AssetAddress = "Assets/Prefabs/AI/VOX/" + msg.VoxMessage  + ".prefab" }, new AudioSourceParameters( spatialBlend: 1));
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