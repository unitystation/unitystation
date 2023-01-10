using UnityEngine;
using Mirror;
using CameraEffects;
using Messages.Server;
using Items.Implants.Organs;
using System.Collections.Generic;

namespace Player
{
	public class PlayerFlashEffects : NetworkBehaviour
	{
		public List<WeldingShieldImplant> WeldingShieldImplants = new List<WeldingShieldImplant>();

		[Server]
		public void ServerSendMessageToClient(GameObject client, float newValue)
		{
			if (WeldingShieldImplants.Count > 0) return;

			PlayerDrunkServerMessage.Send(client, newValue);
			PlayerFlashEffectsMessage.Send(client, newValue);
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