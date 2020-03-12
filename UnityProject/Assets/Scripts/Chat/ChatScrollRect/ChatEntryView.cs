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
	public int Index { get; private set; }
	public bool SpawnedOutside { get; private set; }

	/// <summary>
	/// The current message of the ChatEntry
	/// </summary>
	public string Message => visibleText.text;

	/// <summary>
	/// Get the currently loaded entry data
	/// </summary>
	public ChatEntryData EntryData => entryData;

	public void SetChatEntryView(ChatEntryData data, ChatScrollRect chatScroll, Transform markerBottom,
		Transform markerTop, int index)
	{
		Index = index;
		chatScrollRect = chatScroll;
		tMarkerBottom = markerBottom;
		tMarkerTop = markerTop;
		isResetting = true;
		visibleText.text = data.Message;
		entryData = data;
		gameObject.SetActive(true);

		var bottomTest = transform.position - tMarkerBottom.position;
		var topTest = transform.position - tMarkerTop.position;
		if (bottomTest.y < -20f || topTest.y > 20f)
		{
			SpawnedOutside = true;
		}
		else
		{
			SpawnedOutside = false;
		}
		isResetting = false;
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
		var topTest = transform.position - tMarkerTop.position;

		if (SpawnedOutside)
		{
			if (bottomTest.y > 0f || topTest.y < 0f) SpawnedOutside = false;
		}
		else
		{
			if (bottomTest.y < -20f) ReturnToPool(true);
			if (topTest.y > 20f) ReturnToPool();
		}
	}

	public void ReturnToPool(bool isExitBottom = false)
	{
		isResetting = true;
		chatScrollRect.ReturnViewToPool(this, isExitBottom);
	}
}
