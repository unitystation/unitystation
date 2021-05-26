using Messages.Server;
using UnityEngine;

namespace Doors
{
	public abstract class DoorAnimator : MonoBehaviour
	{
		public DoorController doorController;

		public abstract void OpenDoor(bool skipAnimation);
		public abstract void CloseDoor(bool skipAnimation);
		public abstract void AccessDenied(bool skipAnimation);
		public abstract void PressureWarn(bool skipAnimation);

		/// <summary>
		/// Play the specified animation
		/// </summary>
		/// <param name="type">animation to play</param>
		/// <param name="skipAnimation">if true, animation should be skipped to the end and no sound should be
		/// 	played - currently only used for when players are joining and there are open doors to sync.</param>
		public void PlayAnimation(DoorUpdateType type, bool skipAnimation)
		{
			switch (type)
			{
				case DoorUpdateType.Open:
					OpenDoor(skipAnimation);
					break;
				case DoorUpdateType.Close:
					CloseDoor(skipAnimation);
					break;
				case DoorUpdateType.AccessDenied:
					AccessDenied(skipAnimation);
					break;
				case DoorUpdateType.PressureWarn:
					PressureWarn(skipAnimation);
					break;
			}
		}
	}
}
