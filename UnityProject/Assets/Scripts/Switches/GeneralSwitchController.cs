using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Used for other components to subscribe to an event for when a general switch that is connected to this is pressed.
/// </summary>
public class GeneralSwitchController : MonoBehaviour
{
	public UnityEvent SwitchPressedDoAction = new UnityEvent();
}
