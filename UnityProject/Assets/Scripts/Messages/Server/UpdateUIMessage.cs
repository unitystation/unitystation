using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that updates the hud of the Client's UI sent from the server
/// </summary>
public class UpdateUIMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateUIMessage;
	public int CurHealth;

	public override IEnumerator Process()
	{
		UIManager.PlayerHealthUI.UpdateHealthUI(this, CurHealth);
		yield return null;
	}

	/// <summary>
	///     At the moment it is used to pass the current health of the player
	///     to the players UI from the server. This should be expanded for other
	///     UI related things later
	/// </summary>
	/// <param name="recipient">Recipient.</param>
	/// <param name="cHealth">Current server health.</param>
	public static UpdateUIMessage Send(GameObject recipient, int cHealth)
	{
		UpdateUIMessage msg = new UpdateUIMessage
		{
			CurHealth = cHealth
		};
		msg.SendTo(recipient);
		return msg;
	}
}