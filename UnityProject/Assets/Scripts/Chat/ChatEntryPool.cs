using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatEntryPool : MonoBehaviour
{
	[SerializeField]
	private ChatUI chatInstance;
	private const int INITIAL_POOL_SIZE = 20;

	void Start()
	{
		pooledEntries = new Queue<GameObject>();
		FillUpPool();
	}

	private void FillUpPool()
	{
		for (int i = 0; i < INITIAL_POOL_SIZE; i++)
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
		entry.gameObject.SetActive(true);
		return entry;
	}

	private Queue<GameObject> pooledEntries;
}
