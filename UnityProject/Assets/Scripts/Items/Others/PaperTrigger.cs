using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperTrigger : PickUpTrigger
{
	public NetTabType NetTabType;
	public Paper paper;

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

	public override void UI_InteractOtherSlot(GameObject originator, GameObject otherHandItem)
	{
		if (otherHandItem != null)
		{
			var pen = otherHandItem.GetComponent<Pen>();
			if (pen != null)
			{
				UI_Interact(originator, UIManager.Hands.OtherSlot.eventName);
			}
		}
	}
}