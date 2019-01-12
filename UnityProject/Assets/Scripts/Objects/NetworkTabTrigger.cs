using UnityEngine;

public abstract class NetworkTabTrigger : InputTrigger {
    public NetTabType NetTabType;
    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        var playerScript = originator.GetComponent<PlayerScript>();
        if (playerScript.canNotInteract() || !playerScript.IsInReach( gameObject ))
        { //check for both client and server
            return true;
        }
        if (!isServer)
        { 
            //Client wants this code to be run on server
            InteractMessage.Send(gameObject, hand);
        }
        else
        {
            //Server actions
            TabUpdateMessage.Send( originator, gameObject, NetTabType, TabAction.Open );
            
        }

		return true;
    }
}
