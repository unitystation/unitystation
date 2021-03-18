using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;

public class HealthDollIpdate : ServerMessage<HealthDollIpdate.NetMessage>
{

	public struct NetMessage : NetworkMessage
	{
		public string damageColor;
		public string bodyPartColor;
		public int Location;
	}


    public override void Process(NetMessage msg)
    {
	    UIManager.PlayerHealthUI.bodyPartListeners[msg.Location].SetBodyPartColor(msg.bodyPartColor.UncompresseToColour());
	    UIManager.PlayerHealthUI.bodyPartListeners[msg.Location].SetDamageColor(msg.damageColor.UncompresseToColour());
    }

    public static NetMessage SendTo(int inLocation, Color INdamageColor, Color INbodyPartColor, GameObject ToWho)
    {
	    NetMessage msg = new NetMessage
	    {
		    damageColor = INdamageColor.ToStringCompressed(),
		    bodyPartColor = INbodyPartColor.ToStringCompressed(),
		    Location = inLocation
	    };

	    SendTo(ToWho, msg);
	    return msg;
    }
}
