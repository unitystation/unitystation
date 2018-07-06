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

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			var playerScript = originator.GetComponent<PlayerScript>();
			if (playerScript.canNotInteract() || !playerScript.IsInReach( gameObject ))
			{ //check for both client and server
				return;
			}
			if (!isServer)
			{ 
				//Client wants this code to be run on server
				InteractMessage.Send(gameObject, hand);
			}
			else
			{
				//Server actions
				// Close the door if it's open
				if (Controller.IsOpened && allowInput && !hand.Equals( "bodyBump" ))
				{
					Controller.TryClose();

					allowInput = false;
					StartCoroutine(DoorInputCoolDown());
				}
	
				// Attempt to open if it's closed
				if (Controller != null && allowInput)
				{
					Controller.CheckDoorPermissions(originator, hand);
	
					allowInput = false;
					StartCoroutine(DoorInputCoolDown());
				}
			}
		}

		private IEnumerator DoorInputCoolDown()
		{
			yield return new WaitForSeconds(0.3f);
			allowInput = true;
		}
	}
}