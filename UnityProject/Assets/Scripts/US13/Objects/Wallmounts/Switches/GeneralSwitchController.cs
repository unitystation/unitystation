using UnityEngine;
using UnityEngine.Events;

namespace US13.Objects.Wallmounts.Switches
{
	/// <summary>
	/// Used for other components to subscribe to an event for when a general switch that is connected to this is pressed.
	/// </summary>
	public class GeneralSwitchController : MonoBehaviour
	{
		public UnityEvent SwitchPressedDoAction = new UnityEvent();
	}
}
