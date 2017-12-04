using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
/// <summary>
/// Message that updates the hud of the Client's UI sent from the server
/// </summary>
public class UpdateUIMessage : ServerMessage<UpdateUIMessage>
{
    public NetworkInstanceId Recipient;
    public int CurHealth;
    public override IEnumerator Process()
    {
        yield return WaitFor(Recipient);
        UI.UIManager.PlayerHealthUI.UpdateHealthUI(this, CurHealth);
    }

    /// <summary>
    /// At the moment it is used to pass the current health of the player
    /// to the players UI from the server. This should be expanded for other
    /// UI related things later
    /// </summary>
    /// <param name="recipient">Recipient.</param>
    /// <param name="cHealth">Current server health.</param>
    public static UpdateUIMessage Send(GameObject recipient, int cHealth)
    {
        var msg = new UpdateUIMessage
        {
            Recipient = recipient.GetComponent<NetworkIdentity>().netId,
            CurHealth = cHealth
        };
        msg.SendTo(recipient);
        return msg;
    }
}
