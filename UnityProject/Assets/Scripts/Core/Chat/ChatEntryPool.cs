using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Chat_UI;

public class ChatEntryPool : MonoBehaviour
{
	[SerializeField]
	private ChatUI chatInstance = null;

	void Start()
	{
		pooledEntries = new Queue<GameObject>();
		FillUpPool();
	}

	private void FillUpPool()
	{
		for (int i = 0; i < chatInstance.maxLogLength + 5; i++)
		{
			var entry = Instantiate(chatInstance.chatEntryPrefab, Vector3.zero, Quaternion.identity,
				chatInstance.content);
			entry.SetActive(false);
			pooledEntries.Enqueue(entry);

		}
	}

	public GameObject GetChatEntry()
	{
		if (pooledEntries.Count == 0)
		{
			FillUpPool();
		}

		var entry = pooledEntries.Dequeue();
		entry.SetActive(true);
		return entry;
	}

	public void ReturnChatEntry(GameObject entry)
	{
		pooledEntries.Enqueue(entry);
		entry.SetActive(false);
	}

	private Queue<GameObject> pooledEntries;
}
