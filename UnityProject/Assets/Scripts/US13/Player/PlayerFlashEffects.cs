using Mirror;
using UnityEngine;
using US13.Core.Camera;
using US13.Messages.Server;

namespace US13.Player
{
	public class PlayerFlashEffectsMessage : ServerMessage<PlayerFlashEffectsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float FlashValue;
		}

		public override void Process(NetMessage msg)
		{
			var camera = Camera.main;
			if (camera == null) return;
			camera.GetComponent<CameraEffectControlScript>().FlashEyes(msg.FlashValue);
		}



		/// <summary>
		/// Send full update to a client
		/// </summary>
		public static NetMessage Send(GameObject clientConn, float newflashValue)
		{
			NetMessage msg = new NetMessage
			{
				FlashValue = newflashValue,
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}