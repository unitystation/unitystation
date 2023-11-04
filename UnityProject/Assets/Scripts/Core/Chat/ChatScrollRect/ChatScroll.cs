using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
	[SerializeField] private TMP_InputField TMPinputField = null;

	[SerializeField] private GameObject defaultChatEntryPrefab = null;
	[SerializeField] private Scrollbar scrollBar = null;
	[SerializeField] private float scrollSpeed = 0.5f;
	[SerializeField] private RectTransform layoutRoot = null;

	private List<ChatEntryData> chatLog = new List<ChatEntryData>();
	protected List<ChatEntryView> chatViewPool = new List<ChatEntryView>();
	//Pool of entries that are currently visible with index 0 being the bottom
	protected List<ChatEntryView> displayPool = new List<ChatEntryView>();

	[Tooltip("the max amount of views to display in the view. This would be how many " +
	         "of the minimum sized entries until it touches the top of your viewport")]
	[SerializeField] protected int MaxViews = 17;
	[Tooltip("If this is set to true then the input field will not add a chat entry to the" +
	         "chatlogs. You do this because you want to handle the entry manually")]
	[SerializeField] private bool doNotAddInputToChatLog = false;

	private float contentWidth;

	/// <summary>
	/// Subscribe to this event to receive the inputfield text on submit
	/// </summary>
	public event Action<string> OnInputFieldSubmit;

	private float scrollTime;
	private bool isUsingScrollBar;
	protected bool isInit = false;

	void Awake()
	{
		InitPool();
		contentWidth = chatContentParent.GetComponent<RectTransform>().rect.width;
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UIManager.IsInputFocus = false;
		UIManager.PreventChatInput = false;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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
		ReturnAllViewsToPool();
		chatLog = new List<ChatEntryData>(chatLogsToLoad);
		if (gameObject.activeInHierarchy)
		{
			StartCoroutine(LoadAllChatEntries());
		}
	}

	public virtual void ReturnAllViewsToPool()
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

		var count = 0;
		for (int i = chatLog.Count - 1; i >= 0; i--)
		{
			TryShowView(chatLog[i], false, i);

			count++;
			if (count == MaxViews) break;
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

	protected void TryShowView(ChatEntryData data, bool forBottom, int proposedIndex, ScrollButtonDirection scrollDir = ScrollButtonDirection.None)
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

	public void RebuildLayoutGroup()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
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

	public virtual void OnScrollUp()
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

	public virtual void OnScrollDown()
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

		if (inputField != null)
		{
			if (string.IsNullOrEmpty(inputField.text)) return;

			if (!doNotAddInputToChatLog)
			{
				AddNewChatEntry(new ChatEntryData
				{
					Message = $"You: { GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " + inputField.text}"
				});
			}

			if(OnInputFieldSubmit != null) OnInputFieldSubmit.Invoke(inputField.text);

			inputField.text = "";
			inputField.ActivateInputField();
			StartCoroutine(AfterSubmit());
		}
		else if (TMPinputField != null)
		{
			if (string.IsNullOrEmpty(TMPinputField.text)) return;

			if (!doNotAddInputToChatLog)
			{
				AddNewChatEntry(new ChatEntryData
				{
					Message = $"You: { GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " + TMPinputField.text}"
				});
			}

			if(OnInputFieldSubmit != null) OnInputFieldSubmit.Invoke(TMPinputField.text);

			TMPinputField.text = "";
			TMPinputField.ActivateInputField();
			StartCoroutine(AfterSubmit());
		}

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

	void UpdateMe()
	{
		if(isUsingScrollBar) DetermineScrollRate();
		if (inputField != null)
		{
			if (inputField.IsFocused && KeyboardInputManager.IsEnterPressed())
			{
				OnInputSubmit();
			}
		}
		else if (TMPinputField != null)
		{
			if (TMPinputField.text.Length > 0 && KeyboardInputManager.IsEnterPressed())
			{
				OnInputSubmit();
			}
		}


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
