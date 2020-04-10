using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Message that pops up for client in a window
/// </summary>
public class InfoWindowMessage : ServerMessage
{
	public string Text;
	public string Title;
	public bool Bwoink;
	public uint Recipient;

	public override void Process()
	{
		//To be run on client
//		Logger.Log($"Processed {this}");
		LoadNetworkObject(Recipient);
		UIManager.InfoWindow.Show(Text, Bwoink, string.IsNullOrEmpty(Title) ? "" : Title);
	}

	public static InfoWindowMessage Send(GameObject recipient, string text, string title = "", bool bwoink = true)
	{
		InfoWindowMessage msg =
			new InfoWindowMessage {
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				Text = text,
				Title = title,
				Bwoink = bwoink
			};

		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return $"[InfoWindowMessage Recipient={Recipient} Title={Title} InfoText={Text} Bwoink={Bwoink}]";
	}
}