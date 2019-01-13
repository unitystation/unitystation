using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that updates the hud of the Client's UI sent from the server
/// </summary>
public class UpdateUIMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.UpdateUIMessage;
	public float CurHealth;
	public bool ShowOxyWarning;

	public override IEnumerator Process()
	{
		if (CurHealth == -1)
		{
			UIManager.Instance.oxygenAlertImg.enabled = ShowOxyWarning;
		}
		else
		{
			UIManager.PlayerHealthUI.UpdateHealthUI(this, CurHealth);
		}
		yield return null;
	}

	/// Update health meter for given player
	/// <param name="recipient">Recipient.</param>
	/// <param name="cHealth">Current server health.</param>
	public static UpdateUIMessage SendHealth(GameObject recipient, float cHealth)
	{
		UpdateUIMessage msg = new UpdateUIMessage
		{
			CurHealth = cHealth
		};
		msg.SendTo(recipient);
		return msg;
	}

	/// Show/hide oxygen warning for given player
	/// <param name="recipient">Player to send to</param>
	/// <param name="showOxy">Show oxygen warning</param>
	public static UpdateUIMessage SendOxyWarning(GameObject recipient, bool showOxy)
	{
		UpdateUIMessage msg = new UpdateUIMessage
		{
			CurHealth = -1,
			ShowOxyWarning = showOxy
		};
		msg.SendTo(recipient);
		return msg;
	}
}