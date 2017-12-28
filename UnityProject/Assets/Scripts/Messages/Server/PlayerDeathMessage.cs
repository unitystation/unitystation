using System.Collections;
using UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerDeathMessage : ServerMessage<PlayerDeathMessage> {

    public NetworkInstanceId Recipient;
    public static short MessageType = (short) MessageTypes.PlayerDeathMessage;

    public override IEnumerator Process()
    {
        yield return WaitFor(Recipient);
       UIManager.SetDeathVisibility(false);
    }

    /// <summary>
    ///     Sends the death message
    /// </summary>
    public static PlayerDeathMessage Send(GameObject recipient)
    {
        PlayerDeathMessage msg = new PlayerDeathMessage{ 
            Recipient = recipient.GetComponent<NetworkIdentity>().netId,
        };
        msg.SendTo(recipient);
        return msg;
    }
}
