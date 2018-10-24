using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StorageItem : NetworkBehaviour
{
	[HideInInspector]
	public StorageProps storageProps;
	public int inventorySlots = 7;

	public Action clientUpdatedDelegate;
	void Awake()
	{
		storageProps = new StorageProps(inventorySlots);
	}

	public void UpdateClient(string data)
	{
		storageProps = JsonUtility.FromJson<StorageProps>(data);
		StartCoroutine(storageProps.RefreshItems(clientUpdatedDelegate));
	}

	[Server]
	public void NotifyPlayer(GameObject recipient)
	{
		StorageItemSyncMessage.Send(recipient, gameObject, JsonUtility.ToJson(storageProps));
	}
}

[Serializable]
public class StorageProps
{
	public int slotCount = 7; //Please use lots of 7 for bags and belts

	public List<NetworkInstanceId> inventoryInstances = new List<NetworkInstanceId>();

	[NonSerialized]
	public List<GameObject> inventoryItems = new List<GameObject>(); //Make sure indexes of items match the slots in order

	public StorageProps(int _slotCount)
	{
		slotCount = _slotCount;

		//Populate the lists with null values:
		for (int i = 0; i < slotCount; i++)
		{
			inventoryInstances.Add(NetworkInstanceId.Invalid);
			inventoryItems.Add(null);
		}
	}

	public IEnumerator RefreshItems(Action clientUpdateAction)
	{
		inventoryItems.Clear();

		for (int i = 0; i < inventoryInstances.Count; i++)
		{
			if (inventoryInstances[i] == NetworkInstanceId.Invalid)
			{
				inventoryItems.Add(null);
				continue;
			}
			var findObj = ClientScene.FindLocalObject(inventoryInstances[i]);
			if (findObj != null)
			{
				inventoryItems.Add(findObj);
			}
			else
			{
				inventoryItems.Add(null);
			}

			yield return YieldHelper.EndOfFrame;
		}
		yield return YieldHelper.EndOfFrame;

		if (clientUpdateAction != null)
		{
			clientUpdateAction.Invoke();
		}
	}
}