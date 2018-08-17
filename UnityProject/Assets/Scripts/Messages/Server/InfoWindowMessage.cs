using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that pops up for client in a window
/// </summary>
public class InfoWindowMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.InfoWindowMessage;
	public string Text;
	public string Title;
	public bool Bwoink;
	public NetworkInstanceId Recipient;

	public override IEnumerator Process()
	{
		//To be run on client
//		Logger.Log($"Processed {this}");
		yield return WaitFor(Recipient); //FIXME: broken
//		UIManager.Display.infoWindow.GetComponent<GUI_Info>().Show(Text, Bwoink, string.IsNullOrEmpty(Title) ? "" : Title);
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