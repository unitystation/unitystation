using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class PaperTrigger : InputTrigger
{
	public NetTabType NetTabType;
	public Paper paper;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//TODO: Remove after IF2 refactor
		return false;
	}

	public override void UI_Interact(GameObject originator, string hand)
	{
		var playerScript = originator.GetComponent<PlayerScript>();
		if (!isServer)
		{
			//Client wants this code to be run on server
			InteractMessage.Send(gameObject, hand, true);
		}
		else
		{
			//Server actions
			TabUpdateMessage.Send(originator, gameObject, NetTabType, TabAction.Open);
			paper.UpdatePlayer(originator);
		}
	}

	public override bool UI_InteractOtherSlot(GameObject originator, GameObject otherHandItem)
	{
		if (otherHandItem != null)
		{
			var pen = otherHandItem.GetComponent<Pen>();
			if (pen != null)
			{
				UI_Interact(originator, UIManager.Hands.OtherSlot.eventName);
			}
		}
		return false;
	}
}