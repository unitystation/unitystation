using System;
using System.Collections;
using System.Collections.Generic;
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
	[SerializeField] private Transform markerTop = null;
	[SerializeField] private Transform markerBottom = null;


	private List<ChatEntryData> chatLog = new List<ChatEntryData>();
	private List<ChatEntryView> chatViewPool = new List<ChatEntryView>();
	//Pool of entries that are currently visible with index 0 being the bottom
	private List<ChatEntryView> displayPool = new List<ChatEntryView>();

	private int bottomIndex;
	private int topIndex;
	private float contentWidth;

	public UnityEvent<string> OnInputFieldSubmit;

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
		chatLog.Clear();
		chatLog = new List<ChatEntryData>(chatLogsToLoad);

		StartCoroutine(LoadAllChatEntries());
	}

	/// <summary>
	/// This adds a new chat entry and displays it at the bottom of the scroll feed
	/// </summary>
	public void AddNewChatEntry(ChatEntryData chatEntry)
	{
		TryShowView(chatEntry, true, 0);
	}

	IEnumerator LoadAllChatEntries()
	{
		while (!isInit)
		{
			yield return WaitFor.EndOfFrame;
		}

		foreach (var v in displayPool)
		{
			ReturnViewToPool(v, true, true);
		}

		displayPool.Clear();

		for (int i = 0; i < chatLog.Count; i++)
		{
			if (!TryShowView(chatLog[i], false, i))
			{
				break;
			}
		}
	}

	ChatEntryView InstantiateChatView()
	{
		var obj = Instantiate(defaultChatEntryPrefab, chatContentParent);
		obj.transform.localScale = Vector3.one;
		obj.SetActive(false);
		return obj.GetComponent<ChatEntryView>();
	}

	void ForceViewRefresh()
	{
		foreach (var v in displayPool)
		{
			v.SetChatEntryView(chatLog[v.Index], this, markerBottom, markerTop, v.Index, contentWidth);
		}
	}

	public void ReturnViewToPool(ChatEntryView view, bool isExitBottom, bool onlyRemove = false)
	{
		var data = view.EntryData;
		var dataIndex = chatLog.IndexOf(data);
		view.gameObject.SetActive(false);
		displayPool.Remove(view);

		if (onlyRemove) return;

		if (chatLog.Count == 0) return;

		if (isExitBottom)
		{
			if (dataIndex == chatLog.Count - 1) return;
			bottomIndex = Mathf.Clamp(dataIndex + 1, 0, chatLog.Count - 1);

			if ((topIndex + 1) < chatLog.Count)
			{
				TryShowView(chatLog[topIndex + 1], false, topIndex + 1);
			}
		}
		else
		{
			if (dataIndex == 0) return;
			topIndex = Mathf.Clamp(dataIndex - 1, 0, chatLog.Count - 1);
			if ((bottomIndex - 1) >= 0)
			{
				TryShowView(chatLog[bottomIndex - 1], true, bottomIndex - 1);
			}
		}
	}

	bool TryShowView(ChatEntryData data, bool forBottom, int proposedIndex)
	{
		if (displayPool.Count > 1)
		{
			if (forBottom)
			{
				if (displayPool[0].SpawnedOutside ||
				    displayPool[0].EntryData == data)
					return false;
			}
			else
			{
				if (displayPool[displayPool.Count - 1].SpawnedOutside ||
				    displayPool[displayPool.Count - 1].EntryData == data)
					return false;
			}
		}

		var entry = GetViewFromPool();

		if (forBottom)
		{
			entry.transform.SetAsLastSibling();
			bottomIndex = proposedIndex;
			displayPool.Insert(0, entry);
		}
		else
		{
			entry.transform.SetAsFirstSibling();
			displayPool.Add(entry);
			topIndex = proposedIndex;
		}

		entry.SetChatEntryView(data, this, markerBottom, markerTop, proposedIndex, contentWidth);
		return true;
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

//		AddMessageToLogs(selectedPlayer.PlayerData.uid, $"You: {inputField.text}");
//		RefreshChatLog(selectedPlayer.PlayerData.uid);
//		var message = $"{PlayerManager.CurrentCharacterSettings.username}: {inputField.text}";
//		RequestAdminBwoink.Send(ServerData.UserID, PlayerList.Instance.AdminToken, selectedPlayer.PlayerData.uid,
//			message);
		AddNewChatEntry(new ChatEntryData
		{
			Message = $"You: {inputField.text}"
		});

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

}
