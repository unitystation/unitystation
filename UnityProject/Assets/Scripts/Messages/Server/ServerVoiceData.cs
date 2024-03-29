using System.Collections;
using System.Collections.Generic;
using Adrenak.BRW;
using Logs;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class ServerVoiceData : ServerMessage<ServerVoiceData.UniVoiceMessage>
	{
		public struct UniVoiceMessage : NetworkMessage
		{
			public uint Object;
			public short recipient;
			public byte[] data;
		}
		public override void Process(UniVoiceMessage msg)
		{

			if (VoiceChatManager.Instance == null)
			{
				var bytes = msg.data;
				var packet = new BytesReader(bytes);
				var tag = packet.ReadString();
				if (tag != "AUDIO_SEGMENT")
				{
					VoiceChatManager.CachedMessage.Add(msg);
				}
			}
			else
			{
				if (VoiceChatManager.CachedMessage != null)
				{
					foreach (var Message in VoiceChatManager.CachedMessage)
					{
						VoiceChatManager.Instance.Client_OnMessage(Message);
					}

					VoiceChatManager.CachedMessage.Clear();
				}
				VoiceChatManager.Instance.Client_OnMessage(msg);
			}

		}




		public static UniVoiceMessage Send( UniVoiceMessage msg)
		{
			NetworkServer.SendToAll(msg, Mirror.Channels.Unreliable, sendToReadyOnly: true);
			return msg;
		}

	}
}