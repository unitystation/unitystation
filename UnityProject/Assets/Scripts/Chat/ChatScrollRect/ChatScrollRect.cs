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
	[SerializeField] private GameObject defaultChatEntryPrefab;

	private List<ChatEntryData> chatLog = new List<ChatEntryData>();
	private List<ChatEntryView> chatViewPool = new List<ChatEntryView>();
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

	public void ReturnViewToPool(ChatEntryView view, bool isExitBottom)
	{
		var data = view.EntryData;
		view.gameObject.SetActive(false);
		var dataIndex = chatLog.IndexOf(data);

		if (isExitBottom)
		{
			if (dataIndex == 0)
			{
				bottomIndex = 0;
				return;
			}
		}
		else
		{
			if (dataIndex == chatLog.Count - 1)
			{
				topIndex = chatLog.Count - 1;
				return;
			}
		}
	}
}
