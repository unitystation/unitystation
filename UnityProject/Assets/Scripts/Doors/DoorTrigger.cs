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
		if (Controller == null || !allowInput)
		{
			return true;
		}
		var playerScript = originator.GetComponent<PlayerScript>();
		if (playerScript.canNotInteract() || !playerScript.IsInReach(gameObject))
		{ //check for both client and server
			return true;
		}
		if (!isServer)
		{
			//Client wants this code to be run on server
			InteractMessage.Send(gameObject, interactSlot);
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
					return true;
				}
				Controller.TryClose();
			}
			else
			{
				// Attempt to open if it's closed
				Controller.TryOpen(originator, interactSlot);//fixme: hand can be null
			}
		}
		allowInput = false;
		StartCoroutine(DoorInputCoolDown());
		return true;
	}
	/// Disables any interactions with door for a while
	private IEnumerator DoorInputCoolDown()
	{
		yield return new WaitForSeconds(0.3f);
		allowInput = true;
	}
}
