using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatEntryView : MonoBehaviour
{
	[SerializeField] private Text visibleText = null;
	[SerializeField] private RectTransform textRectTransform = null;
	[SerializeField] private RectTransform thisRectTransform = null;
	protected ChatEntryData entryData;
	protected ChatScroll chatScroll;

	public int Index { get; private set; }

	/// <summary>
	/// The current message of the ChatEntry
	/// </summary>
	public string Message => visibleText.text;

	/// <summary>
	/// Get the currently loaded entry data
	/// </summary>
	public ChatEntryData EntryData => entryData;

	public virtual void SetChatEntryView(ChatEntryData data, ChatScroll chatScroll, int index, float contentViewWidth)
	{
		var thisDelta = thisRectTransform.sizeDelta;
		thisDelta.x = contentViewWidth;
		thisRectTransform.sizeDelta = thisDelta;
		var textDelta = textRectTransform.sizeDelta;
		textDelta.x = contentViewWidth / textRectTransform.localScale.x;
		textRectTransform.sizeDelta = textDelta;

		Index = index;
		this.chatScroll = chatScroll;
		visibleText.text = data.Message;
		entryData = data;
		gameObject.SetActive(true);

		StartCoroutine(UpdateMinHeight());
	}

	IEnumerator UpdateMinHeight()
	{
		yield return WaitFor.EndOfFrame;
		var thisRectDelta = thisRectTransform.sizeDelta;
		thisRectDelta.y = textRectTransform.rect.height * textRectTransform.localScale.y;
		thisRectTransform.sizeDelta = thisRectDelta;
		chatScroll.RebuildLayoutGroup();
	}
}