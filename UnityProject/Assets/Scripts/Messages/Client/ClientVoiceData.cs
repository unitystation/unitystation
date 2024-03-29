using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	public class ClientVoiceData : ClientMessage<ClientVoiceData.UniVoiceMessage>
	{
		public struct UniVoiceMessage : NetworkMessage
		{
			public short recipient;
			public byte[] data;
		}

		public override void Process(UniVoiceMessage msg)
		{
			VoiceChatManager.Instance.Server_OnMessage(SentByPlayer.Connection, msg);
		}

		public static UniVoiceMessage Send( UniVoiceMessage msg)
		{
			NetworkClient.Send(msg, Mirror.Channels.Unreliable);
			return msg;
		}
	}
}