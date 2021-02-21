using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Message that pops up for client in a window
/// </summary>
public class InfoWindowMessage : ServerMessage
{
	public struct InfoWindowMessageNetMessage : NetworkMessage
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

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public InfoWindowMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		//To be run on client
		var newMsgNull = msg as InfoWindowMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.Recipient);
		UIManager.InfoWindow.Show(newMsg.Text, newMsg.Bwoink, string.IsNullOrEmpty(newMsg.Title) ? "" : newMsg.Title);
	}

	public static InfoWindowMessageNetMessage Send(GameObject recipient, string text, string title = "", bool bwoink = true)
	{
		InfoWindowMessageNetMessage msg =
			new InfoWindowMessageNetMessage {
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				Text = text,
				Title = title,
				Bwoink = bwoink
			};

		new InfoWindowMessage().SendTo(recipient, msg);
		return msg;
	}
}