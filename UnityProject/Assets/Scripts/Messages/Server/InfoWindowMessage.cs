using System.Collections;
using UI;
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
	public Color Color;
	public NetworkInstanceId Recipient;

	public override IEnumerator Process()
	{
		//To be run on client
//		Debug.Log($"Processed {this}");
		yield return WaitFor(Recipient);
		UIManager.Display.infoWindow.GetComponent<GUI_Info>().Show(Text, Color, string.IsNullOrEmpty(Title) ? "" : Title);
	}

	public static InfoWindowMessage Send(GameObject recipient, string text, string title = "")
	{
		return Send(recipient, text, title, GUI_Info.infoColor);
	}

	public static InfoWindowMessage Send(GameObject recipient, string text, string title, Color color)
	{
		InfoWindowMessage msg =
			new InfoWindowMessage {
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				Text = text, 
				Title = title,
				Color = color
			};

		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return $"[InfoWindowMessage Recipient={Recipient} Title={Title} InfoText={Text} Color={Color}]";
	}
}