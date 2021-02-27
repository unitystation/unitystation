using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	///     Message that pops up for client in a window
	/// </summary>
	public class InfoWindowMessage : ServerMessage<InfoWindowMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Text;
			public string Title;
			public bool Bwoink;
			public uint Recipient;

			public override string ToString()
			{
				return $"[InfoWindowMessage Recipient={Recipient} Title={Title} InfoText={Text} Bwoink={Bwoink}]";
			}
		}

		public override void Process(NetMessage msg)
		{
			//To be run on client
			LoadNetworkObject(msg.Recipient);
			UIManager.InfoWindow.Show(msg.Text, msg.Bwoink, string.IsNullOrEmpty(msg.Title) ? "" : msg.Title);
		}

		public static NetMessage Send(GameObject recipient, string text, string title = "", bool bwoink = true)
		{
			NetMessage msg =
				new NetMessage {
					Recipient = recipient.GetComponent<NetworkIdentity>().netId,
					Text = text,
					Title = title,
					Bwoink = bwoink
				};

			SendTo(recipient, msg);
			return msg;
		}
	}
}