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
	void Awake()
	{
		storageProps = new StorageProps(inventorySlots);
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
}