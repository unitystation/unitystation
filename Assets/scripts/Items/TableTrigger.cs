using InputControl;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class TableTrigger: InputTrigger {
	public override void Interact(GameObject originator, string hand)
	{
		if ( !isServer && ClientApprove() )
		{
			//Client informs server of interaction attempt
            InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
		}
		else
		{	//Server actions
			if ( !ValidateTableInteraction(originator, hand) )
			{
				//Rollback prediction here
				originator.GetComponent<PlayerNetworkActions>().RollbackPrediction(hand);			
			}
		}
	}

	/// <summary>
	/// Client pre-approval and simulation
	/// </summary>
	private bool ClientApprove()
	{
		return UIManager.Hands.CurrentSlot.CanPlaceItem(gameObject.transform.position);
	}

	[Server]
	private bool ValidateTableInteraction(GameObject originator, string hand)
	{
		var ps = originator.GetComponent<PlayerScript>();
		if ( ps.canNotInteract() || !ps.IsInReach(transform) )
		{
			return false;
		}

		GameObject item = ps.playerNetworkActions.Inventory[hand];
		if ( item == null ) return false;
		var targetPosition = gameObject.transform.position; //Camera.main.ScreenToWorldPoint(Input.mousePosition);
		targetPosition.z = -0.2f;
		ps.playerNetworkActions.PlaceItem(UIManager.Hands.CurrentSlot.eventName,
			targetPosition, gameObject);
		item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);

		return true;
	}

	
}
