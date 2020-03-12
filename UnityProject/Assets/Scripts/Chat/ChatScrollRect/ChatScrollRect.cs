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
	[SerializeField] private Transform chatContentParent = null;
	[SerializeField] private InputField inputField = null;
	[SerializeField] private GameObject defaultChatEntryPrefab = null;
	[SerializeField] private Transform markerTop = null;
	[SerializeField] private Transform markerBottom = null;


	private List<ChatEntryData> chatLog = new List<ChatEntryData>();
	private List<ChatEntryView> chatViewPool = new List<ChatEntryView>();
	//Pool of entries that are currently visible with index 0 being the bottom
	private List<ChatEntryView> displayPool = new List<ChatEntryView>();

	private int bottomIndex;
	private int topIndex;

	void Awake()
	{
		InitPool();
	}

	void InitPool()
	{
		for (int i = 0; i < 20; i++)
		{
			chatViewPool.Add(InstantiateChatView());
		}
	}

	ChatEntryView InstantiateChatView()
	{
		var obj = Instantiate(defaultChatEntryPrefab, chatContentParent);
		obj.transform.localScale = Vector3.one;
		obj.SetActive(false);
		return obj.GetComponent<ChatEntryView>();
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
				TryShowView(chatLog[topIndex + 1], false);
			}
		}
		else
		{
			if (dataIndex == 0) return;
			topIndex = Mathf.Clamp(dataIndex - 1, 0, chatLog.Count - 1);
			if ((bottomIndex - 1) >= 0)
			{
				TryShowView(chatLog[bottomIndex - 1], true);
			}
		}
	}

	void TryShowView(ChatEntryData data, bool forBottom)
	{
		if (displayPool.Count > 1)
		{
			if (forBottom)
			{
				if (displayPool[0].SpawnedOutside ||
				    displayPool[0].EntryData == data)
					return;
			}
			else
			{
				if (displayPool[displayPool.Count - 1].SpawnedOutside ||
				    displayPool[displayPool.Count - 1].EntryData == data)
					return;
			}
		}

		var entry = GetViewFromPool();

		if (forBottom)
		{
			entry.transform.SetAsLastSibling();
			bottomIndex--;
			displayPool.Insert(0, entry);
		}
		else
		{
			entry.transform.SetAsFirstSibling();
			displayPool.Add(entry);
			topIndex++;
		}

		entry.SetChatEntryView(data, this, markerBottom, markerTop);
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
}
