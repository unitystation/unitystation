using InputControl;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class TableTrigger: InputTrigger {
	public override void Interact(GameObject originator, string hand)
	{
		if ( !isServer )
		{    //Client informs server of interaction attempt
			InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
		}
		else
		{	//Server actions
			if ( !ValidateTableInteraction(originator, hand) )
			{
				//Rollback prediction here
//				Debug.Log("Uh-oh, failed table interaction");
			}
		}
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
//		if ( PlayerManager.LocalPlayerScript != null )
//			if ( !PlayerManager.LocalPlayerScript.playerMove.allowInput ||
//			     PlayerManager.LocalPlayerScript.playerMove.isGhost )
//				return;
//
//		if ( PlayerManager.PlayerInReach(transform) )
//		{
//			GameObject item = UIManager.Hands.CurrentSlot.PlaceItemInScene();
//			if ( item != null )
//			{
//				var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//				targetPosition.z = -0.2f;
//				PlayerManager.LocalPlayerScript.playerNetworkActions.PlaceItem(UIManager.Hands.CurrentSlot.slotName,
//					targetPosition, gameObject);
//				item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
//			}
//		}
	}

	
}
