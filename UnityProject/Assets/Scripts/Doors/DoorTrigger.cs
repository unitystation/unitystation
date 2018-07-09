using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UnityEngine;

namespace Doors
{
	/// <summary>
	///     Handles Interact messages from InputController.cs
	///     It also checks for access restrictions on the players ID card
	/// </summary>
	public class DoorTrigger : InputTrigger
	{
		public bool allowInput = true;

		private DoorController controller;
		public DoorController Controller {
			get {
				if ( !controller ) {
					controller = GetComponent<DoorController>();
				}
				return controller;
			}
		}

		public override void Interact(GameObject originator, Vector3 position, string interactSlot = null)
		{
			if ( Controller == null || !allowInput ) {
				return;
			}
			var playerScript = originator.GetComponent<PlayerScript>();
			if (playerScript.canNotInteract() || !playerScript.IsInReach( gameObject ))
			{ //check for both client and server
				return;
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
				if ( Controller.IsOpened )
				{
					//Not even trying to close when bumping into door
					if ( interactSlot == null ) {
						return;
					}
					Controller.TryClose();
				} else {
				// Attempt to open if it's closed
					Controller.TryOpen(originator, interactSlot);
				}
			}
			allowInput = false;
			StartCoroutine(DoorInputCoolDown());
		}
		/// Disables any interactions with door for a while
		private IEnumerator DoorInputCoolDown()
		{
			yield return new WaitForSeconds(0.3f);
			allowInput = true;
		}
	}
}