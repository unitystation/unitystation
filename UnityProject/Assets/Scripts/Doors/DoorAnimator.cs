using System;
using UnityEngine;

namespace Doors
{
	public abstract class DoorAnimator : MonoBehaviour
	{
		public DoorController doorController;


		public abstract void OpenDoor();
		public abstract void CloseDoor();
		public abstract void AccessDenied();

		public void PlayAnimation( DoorUpdateType type ) {
			switch ( type ) {
				case DoorUpdateType.Open:
					OpenDoor();
					break;
				case DoorUpdateType.Close:
					CloseDoor();
					break;
				case DoorUpdateType.AccessDenied:
					AccessDenied();
					break;
			}
		}
	}
}