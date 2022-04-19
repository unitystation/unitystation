using UnityEngine;
using Mirror;
using CameraEffects;
using Messages.Server;

namespace Player
{
	public class PlayerFlashEffects : NetworkBehaviour
	{
		[Server]
		public void ServerSendMessageToClient(GameObject client, float newValue)
		{
			PlayerDrunkServerMessage.Send(client, newValue);
		}
	}

	public class PlayerFlashEffectsMessage : ServerMessage<PlayerFlashEffectsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float flashValue;
		}

		public override void Process(NetMessage msg)
		{
			var camera = Camera.main;
			if (camera == null) return;
			camera.GetComponent<CameraEffectControlScript>().FlashEyes(msg.flashValue);
		}

		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newflashValue)
		{
			NetMessage msg = new NetMessage
			{
				flashValue = newflashValue
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}