using System.Collections;
using UnityEngine;


/// <summary>
///     Handles Interact messages from MouseInputController.cs
///     It also checks for access restrictions on the players ID card
/// </summary>
public class DoorTrigger : InputTrigger
{
	public bool allowInput = true;

	private DoorController controller;
	public DoorController Controller
	{
		get
		{
			if (!controller)
			{
				controller = GetComponent<DoorController>();
			}
			return controller;
		}
	}

	public override bool Interact(GameObject originator, Vector3 position, string interactSlot = null)
	{
		var interacted = false;
		if (Controller == null || !allowInput)
		{
			return false;
		}
		var playerScript = originator.GetComponent<PlayerScript>();
		//Allowing players in soft crit to interact with doors
		if ( (playerScript.canNotInteract() && !playerScript.playerHealth.IsSoftCrit)
		    || !playerScript.IsInReach(gameObject, false))
		{ //check for both client and server
			return false;
		}
		if (!isServer)
		{
			//Client wants this code to be run on server
			InteractMessage.Send(gameObject, interactSlot);
			interacted = true;
		}
		else
		{
			//Server actions
			// Close the door if it's open
			if (Controller.IsOpened)
			{
				//Not even trying to close when bumping into door
				if (interactSlot == null)
				{
					return false;
				}
				Controller.TryClose();
				interacted = true;
			}
			else
			{
				// Attempt to open if it's closed
				Controller.TryOpen(originator, interactSlot);//fixme: hand can be null
				interacted = true;
			}
		}
		allowInput = false;
		StartCoroutine(DoorInputCoolDown());
		return interacted;
	}
	/// Disables any interactions with door for a while
	private IEnumerator DoorInputCoolDown()
	{
		yield return WaitFor.Seconds(0.3f);
		allowInput = true;
	}
}
