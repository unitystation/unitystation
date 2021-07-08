using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class doesn't handle cooking itself; it merely stores the cooking times
/// and products for other objects (e.g. the microwave) to use when cooking.
/// The <see cref="OnCooked"/> event is raised when something cooks this, which other
/// components can subscribe to, to perform extra logic (for e.g. microwaving dice to rig them)
/// </summary>
public class Cookable : MonoBehaviour
{
	[Tooltip("Minimum time to cook.")]
	public int CookTime = 10;

	[Tooltip("What this GameObject becomes when cooked. If not set, this GameObject will not change GameObject when cooked.")]
	public GameObject CookedProduct;

	/// <summary>
	/// Raised when enough cooking time has been added (via <see cref="AddCookingTime(float)"/>)
	/// </summary>
	public event Action OnCooked;

	private float timeSpentCooking;

	/// <summary>
	/// Adds the given cooking time to this object. Will return true if the item is now cooked.
	/// </summary>
	/// <param name="time">The amount of time in seconds to add to this object's time spent cooking.</param>
	/// <returns>true if the added time and any previous time spent cooking was enough to exceed the required cooking time.</returns>
	public bool AddCookingTime(float time)
	{
		timeSpentCooking += time;
		if (timeSpentCooking > CookTime)
		{
			OnCooked?.Invoke();
			return true;
		}

		return false;
	}
}
