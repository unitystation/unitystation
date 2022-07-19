using System.Collections.Generic;
using Managers;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class ServerSimpleAudioPlayMessage : ServerMessage<ServerSimpleAudioPlayMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int ID;
		}

		public override void Process(NetMessage msg)
		{
			SimpleAudioManager.Instance.StartCoroutine(SimpleAudioManager.Instance.Play(msg.ID));
		}

		public static void Send(int idToPlay)
		{
			var msg = new NetMessage
			{
				ID = idToPlay,
			};
			SendToAll(msg);
		}

	}
}