using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShuttleInteract : NetworkTabTrigger 
{
    public string interactionMessage;
    public MatrixMove ShuttleMatrixMove;

    public TabStateEvent OnStateChange;
    private TabState state = TabState.None;
    public TabState State
    {
	    get { return state; }
	    set
	    {
		    if ( state != value )
		    {
			    state = value;
			    OnStateChange.Invoke( value );
		    }
	    }
    }

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
		    if ( State == TabState.None )
		    {
			    State = TabState.Normal;
		    }
		    switch ( State )
		    {
			    case TabState.Normal:
				    if ( UsedEmag( originator, hand ) )
				    {
					    //todo sparks
					    State = TabState.Emagged;
				    }
				    break;
			    case TabState.Emagged:
				    if ( UsedEmag( originator, hand ) )
				    {
					    State = TabState.Off;
				    }
				    break;
			    case TabState.Off:
				    if ( UsedEmag( originator, hand ) )
				    {
					    State = TabState.Normal;
				    }
				    break;
		    }
	    }
	    return true;
    }

    private bool UsedEmag( GameObject originator, string hand )
    {
	    var slot = InventoryManager.GetSlotFromOriginatorHand( originator, hand );
	    var emag = slot.Item?.GetComponent<EmagTrigger>();
	    if ( emag != null )
	    {
		    return true;
	    }

	    return false;
    }
}
public enum TabState
{
	None, Normal, Emagged, Off
}
/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class TabStateEvent : UnityEvent<TabState>{}