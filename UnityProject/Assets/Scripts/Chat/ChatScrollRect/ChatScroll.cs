using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// A specific ScrollRect that also handles the
/// Data binding and display of a collection of
/// chat messages. Loosely based off MVVM where
/// this would be the ViewModel
/// </summary>
public class ChatScroll : MonoBehaviour
{
	[SerializeField] private Transform chatContentParent = null;
	[SerializeField] private InputFieldFocus inputField = null;
	[SerializeField] private GameObject defaultChatEntryPrefab = null;
	[SerializeField] private Scrollbar scrollBar = null;
	[SerializeField] private float scrollSpeed = 0.5f;

	private List<ChatEntryData> chatLog = new List<ChatEntryData>();
	private List<ChatEntryView> chatViewPool = new List<ChatEntryView>();
	//Pool of entries that are currently visible with index 0 being the bottom
	private List<ChatEntryView> displayPool = new List<ChatEntryView>();

	[Tooltip("the max amount of views to display in the view. This would be how many " +
	         "of the minimum sized entries until it touches the top of your viewport")]
	[SerializeField] private int MaxViews = 17;
	[Tooltip("If this is set to true then the input field will not add a chat entry to the" +
	         "chatlogs. You do this because you want to handle the entry manually")]
	[SerializeField] private bool doNotAddInputToChatLog;

	private float contentWidth;

	/// <summary>
	/// Subscribe to this event to receive the inputfield text on submit
	/// </summary>
	public event Action<string> OnInputFieldSubmit;

	private float scrollTime;
	private bool isUsingScrollBar;
	private bool isInit = false;

	void Awake()
	{
		InitPool();
		contentWidth = chatContentParent.GetComponent<RectTransform>().rect.width;
	}

	void InitPool()
	{
		for (int i = 0; i < 20; i++)
		{
			chatViewPool.Add(InstantiateChatView());
		}

		isInit = true;
	}

	/// <summary>
	/// Removes all previous content and reloads the chat with
	/// the new chat logs
	/// </summary>
	/// <param name="chatLogsToLoad">A list of the chatlogs you want to load into the scroll rect</param>
	public void LoadChatEntries(List<ChatEntryData> chatLogsToLoad)
	{
		foreach (Transform t in chatContentParent.transform)
		{
			var c = t.GetComponent<ChatEntryView>();
			if (c != null && t.gameObject.activeInHierarchy)
			{
				ReturnViewToPool(c);
			}
		}

		displayPool.Clear();

		chatLog.Clear();
		chatLog = new List<ChatEntryData>(chatLogsToLoad);

		StartCoroutine(LoadAllChatEntries());
	}

	/// <summary>
	/// Appends new chat logs to an already loaded chat scroll
	/// </summary>
	public void AppendChatEntries(List<ChatEntryData> chatLogsToAppend)
	{
		foreach (var e in chatLogsToAppend)
		{
			AddNewChatEntry(e);
		}
	}

	/// <summary>
	/// This adds a new chat entry and displays it at the bottom of the scroll feed
	/// </summary>
	public void AddNewChatEntry(ChatEntryData chatEntry)
	{
		if (displayPool.Count != 0 && displayPool[0].Index != chatLog.Count - 1)
		{
			chatLog.Add(chatEntry);
			return;
		}
		chatLog.Add(chatEntry);
		TryShowView(chatEntry, true, chatLog.Count - 1);
	}

	IEnumerator LoadAllChatEntries()
	{
		while (!isInit)
		{
			yield return WaitFor.EndOfFrame;
		}

		for (int i = chatLog.Count - 1; i >= 0 && i < MaxViews; i--)
		{
			TryShowView(chatLog[i], false, i);
		}
	}

	ChatEntryView InstantiateChatView()
	{
		var obj = Instantiate(defaultChatEntryPrefab, chatContentParent);
		obj.transform.localScale = Vector3.one;
		obj.SetActive(false);
		return obj.GetComponent<ChatEntryView>();
	}

	public void ReturnViewToPool(ChatEntryView view)
	{
		view.gameObject.SetActive(false);
		if (displayPool.Contains(view))
		{
			displayPool.Remove(view);
		}
	}

	void TryShowView(ChatEntryData data, bool forBottom, int proposedIndex, ScrollButtonDirection scrollDir = ScrollButtonDirection.None)
	{
		var entry = GetViewFromPool();

		if (forBottom)
		{
			entry.transform.SetAsLastSibling();
			displayPool.Insert(0, entry);
		}
		else
		{
			entry.transform.SetAsFirstSibling();
			displayPool.Add(entry);
		}

		if (!gameObject.activeInHierarchy) return;
		entry.SetChatEntryView(data, this, proposedIndex, contentWidth);
		DetermineTrim(scrollDir);
	}

	void DetermineTrim(ScrollButtonDirection scrollDir)
	{
		if (scrollDir != ScrollButtonDirection.None
		    || displayPool.Count <= MaxViews) return;

		for (int i = displayPool.Count - 1; i >= 0; i--)
		{
			if (i < MaxViews) continue;
			ReturnViewToPool(displayPool[i]);
		}
	}

	public void OnScrollUp()
	{
		if (chatLog.Count <= MaxViews) return;
		if (displayPool.Count == 0) return;


		//Player wants to see chat entries further up
		//get the data index of the view at the top of the display list
		var index = displayPool[displayPool.Count - 1].Index;
		//check to see if we can scroll any further up
		if (index == 0) return;
		//if so then spawn a new view at the top and remove on from the bottom
		var proposedIndex = index - 1;
		TryShowView(chatLog[proposedIndex], false, proposedIndex, ScrollButtonDirection.Down);
		ReturnViewToPool(displayPool[0]);
	}

	public void OnScrollDown()
	{
		if (chatLog.Count <= MaxViews) return;
		if (displayPool.Count == 0) return;

		//Player wants to see chat entries further down
		//get the data index of the view on the bottom
		var index = displayPool[0].Index;

		//check to see if we can scroll any further based off this index
		if (index == chatLog.Count - 1) return;
		//if so then spawn a new view at the bottom and remove one from the top
		var proposedIndex = index + 1;

		TryShowView(chatLog[proposedIndex], true, proposedIndex, ScrollButtonDirection.Down);
		ReturnViewToPool(displayPool[displayPool.Count - 1]);
	}

	ChatEntryView GetViewFromPool()
	{
		foreach (var c in chatViewPool)
		{
			if (!c.gameObject.activeInHierarchy)
			{
				return c;
			}
		}

		var newView = InstantiateChatView();
		chatViewPool.Add(newView);
		return newView;
	}

	public void OnInputSubmit()
	{
		if (string.IsNullOrEmpty(inputField.text)) return;

		if (!doNotAddInputToChatLog)
		{
			AddNewChatEntry(new ChatEntryData
			{
				Message = $"You: {inputField.text}"
			});
		}

		if(OnInputFieldSubmit != null) OnInputFieldSubmit.Invoke(inputField.text);

		inputField.text = "";
		inputField.ActivateInputField();
		StartCoroutine(AfterSubmit());
	}

	IEnumerator AfterSubmit()
	{
		yield return WaitFor.EndOfFrame;
		UIManager.IsInputFocus = true;
	}

	public void OnScrollPointerDown()
	{
		isUsingScrollBar = true;
	}

	public void OnScrollPointerUp()
	{
		isUsingScrollBar = false;
		if (displayPool.Count != 0 && displayPool[0].Index != chatLog.Count - 1)
		{
			scrollBar.value = 0.5f;
		}
		else
		{
			scrollBar.value = 0f;
		}
	}

	void Update()
	{
		if(isUsingScrollBar) DetermineScrollRate();
	}

	void DetermineScrollRate()
	{
		scrollTime += Time.deltaTime;

		var scrollValue = scrollBar.value;

		if (scrollValue > 0.45f && scrollValue < 0.55f)
		{
			scrollTime = 0f;
			return;
		}

		var speedMulti = 0.1f;
		ScrollButtonDirection direction = ScrollButtonDirection.None;
		if (scrollValue < 0.45f)
		{
			direction = ScrollButtonDirection.Down;
			speedMulti = Mathf.Lerp(0.1f, 1f, scrollValue / 0.45f);
		}
		else
		{
			direction = ScrollButtonDirection.Up;
			speedMulti = Mathf.Lerp(0.1f, 1f, (1f - scrollValue) / 0.45f);
		}

		if (scrollTime >= scrollSpeed * speedMulti)
		{
			scrollTime = 0f;
			if (direction == ScrollButtonDirection.Up) OnScrollUp();
			if (direction == ScrollButtonDirection.Down) OnScrollDown();
		}
	}
}

public enum ScrollButtonDirection
{
	None,
	Up,
	Down
}
