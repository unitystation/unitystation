using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Doors
{
	/// <summary>
	/// Controls glass sliding doors of all types
	/// </summary>
	public class WinDoorController : DoorController
	{
		 public override void OpenAction(){
			IsOpened = true;

			if (!isPerformingAction) {
				//TODO: Need a doorAnimator for WinDoors
				//doorAnimator.OpenDoor();
				Debug.Log("WinDoor: (WIP) Still requires a specific DoorAnimator class for this type of door.");
			}
		}
	}
}
