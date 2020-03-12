using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatEntryView : MonoBehaviour
{
	[SerializeField] private Text visibleText = null;
	private Transform tMarkerBottom;
	private Transform tMarkerTop;
	private ChatEntryData entryData;
	private ChatScrollRect chatScrollRect;
	private bool isResetting = false;

	/// <summary>
	/// The current message of the ChatEntry
	/// </summary>
	public string Message => visibleText.text;

	/// <summary>
	/// Get the currently loaded entry data
	/// </summary>
	public ChatEntryData EntryData => entryData;

	public void SetChatEntryView(ChatEntryData data, ChatScrollRect chatScroll, Transform markerBottom,
		Transform markerTop)
	{
		chatScrollRect = chatScroll;
		tMarkerBottom = markerBottom;
		tMarkerTop = markerTop;
		isResetting = false;
		visibleText.text = data.Message;
		entryData = data;
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
		if (isResetting) return;

		CheckPosition();
	}

	void CheckPosition()
	{
		var bottomTest = transform.position - tMarkerBottom.position;
		if (bottomTest.y < -20f)
		{
			ReturnToPool(true);
			return;
		}

		var topTest = transform.position - tMarkerTop.position;
		if (topTest.y > 20f)
		{
			ReturnToPool();
			return;
		}
	}

	public void ReturnToPool(bool isExitBottom = false)
	{
		isResetting = true;
		chatScrollRect.ReturnViewToPool(this, isExitBottom);
	}
}
