using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the GUI notification prefab
/// </summary>
public class GUI_Notification : MonoBehaviour
{
	private Dictionary<string, int> notification = new Dictionary<string, int>();
	[SerializeField] private Text label;

	/// <summary>
	/// Adds the amount to the count of the given key
	/// </summary>
	public void AddNotification(string key, int amountToAdd)
	{

	}

	/// <summary>
	/// Removes a key entry and its notification count
	/// </summary>
	/// <param name="key"></param>
	public void RemoveNotification(string key)
	{

	}

	/// <summary>
	/// Removes all notification entries and resets the count to 0
	/// </summary>
	public void ClearAll()
	{

	}
}
