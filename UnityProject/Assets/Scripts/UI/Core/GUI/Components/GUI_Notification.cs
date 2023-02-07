using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the GUI notifications prefab
/// </summary>
public class GUI_Notification : MonoBehaviour
{
	public Dictionary<string, int> notifications = new Dictionary<string, int>();

	[SerializeField] private Text label = null;
	[SerializeField] private Image background = null;
	[SerializeField] private AddressableAudioSource nofticationSound;

	private void OnEnable()
	{
		UpdateText();
	}

	/// <summary>
	/// Adds the amount to the count of the given key
	/// </summary>
	public void AddNotification(string key, int amountToAdd)
	{
		if (!notifications.ContainsKey(key))
		{
			notifications.Add(key, 0);
		}

		notifications[key] += amountToAdd;
		if (notifications[key] < 0)
		{
			notifications[key] = 0;
		}

		if (nofticationSound != null)
		{
			SoundManager.Play(nofticationSound);
		}
		UpdateText();
	}

	/// <summary>
	/// Removes the entry and its notifications count
	/// </summary>
	/// <param name="key"></param>
	public void RemoveNotification(string key)
	{
		if (notifications.ContainsKey(key))
		{
			notifications.Remove(key);
		}

		UpdateText();
	}

	/// <summary>
	/// Removes all notifications entries and resets the count to 0
	/// </summary>
	public void ClearAll()
	{
		notifications.Clear();
		UpdateText();
	}

	private void UpdateText()
	{
		int count = 0;
		foreach (var n in notifications)
		{
			count += Mathf.Clamp(n.Value, 0, 999);
		}

		label.text = count.ToString();
		if (count == 0)
		{
			ToggleUIVisibility(false);
		}
		else
		{
			ToggleUIVisibility(true);
		}
	}

	private void ToggleUIVisibility(bool isOn)
	{
		label.enabled = isOn;
		background.enabled = isOn;
	}
}
