using System.Collections.Generic;
using Managers;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class ServerSimpleAudioManagerListMessage : ServerMessage<ServerSimpleAudioManagerListMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public List<SimpleAudioManager.SimpleAudioData> Data;
		}

		public override void Process(NetMessage msg)
		{
			SimpleAudioManager.Instance.LoadDataFromServer = msg.Data;
			SimpleAudioManager.Instance.StartCoroutine(SimpleAudioManager.Instance.DownloadSounds());
			Debug.Log("updating stuff");
		}

		public static void Send()
		{
			var msg = new NetMessage
			{
				Data = SimpleAudioManager.Instance.LoadDataFromServer,
			};
			SendToAll(msg);
		}
	}
}