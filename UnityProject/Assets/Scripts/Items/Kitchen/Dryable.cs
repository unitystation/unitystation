using System;
using UnityEngine;
using NaughtyAttributes;


public class Dryable : MonoBehaviour
{
	[Tooltip("Minimum time to dry.")]
	public int DryTime = 10;

	[InfoBox("If no transormation is to take place, then don't select any item. Don't select the same item as itself.", EInfoBoxType.Warning)]
	[Tooltip("What this item becomes when dried." +
				"If not set, this item will not change GameObject when dried, but will still invoke the dried event.")]
	public GameObject DriedProduct;

	/// <summary>
	/// Raised when enough drying time has been added (via <see cref="AddDryingTime(float)"/>)
	/// </summary>
	public event Action OnDried;

	private float timeSpentDrying;

	/// <summary>
	/// Adds the given cooking time to this object. Will return true if the item is now cooked.
	/// </summary>
	/// <param name="time">The amount of time in seconds to add to this object's time spent cooking.</param>
	/// <returns>true if the added time and any previous time spent cooking was enough to exceed the required cooking time.</returns>
	public bool AddDryingTime(float time)
	{
	timeSpentDrying += time;
		if (timeSpentDrying > DryTime)
		{
			OnDried?.Invoke();
			return true;
		}
		return false;
	}
}

