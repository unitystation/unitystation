using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A specific ScrollRect that also handles the
/// Data binding and display of a collection of
/// chat messages. Loosely based off MVVM where
/// this would be the ViewModel
/// </summary>
public class ChatScrollRect : ScrollRect
{

}

public class ChatEntryData
{

}

public class ChatEntryView : MonoBehaviour
{
	[SerializeField] private Text visibleText = null;
	private Transform tMarkerBottom;
	private Transform tMarkerTop;

	/// <summary>
	/// The current message of the ChatEntry
	/// </summary>
	public string Message => visibleText.text;

	public void SetChatEntryView(string message)
	{
		visibleText.text = message;
		gameObject.SetActive(true);
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.LATE_UPDATE, LateUpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.LATE_UPDATE, LateUpdateMe);
	}

	void LateUpdateMe()
	{
		CheckPosition();
	}

	void CheckPosition()
	{

	}

	public void ReturnToPool()
	{

	}
}
