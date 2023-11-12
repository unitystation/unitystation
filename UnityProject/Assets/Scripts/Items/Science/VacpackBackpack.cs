using System;
using System.Collections;
using System.Collections.Generic;
using Objects;
using UnityEngine;

public class VacpackBackpack : MonoBehaviour
{
	public ObjectContainer ObjectContainer;

	public int CurrentlyStored;

	public int MaxStored = 10;


	public void Awake()
	{
		ObjectContainer = this.GetComponent<ObjectContainer>();
	}

	public void TryStore(GameObject ToStore)
	{
		if (CurrentlyStored >= MaxStored)
		{
			return;
		}

		CurrentlyStored++;
		ObjectContainer.StoreObject(ToStore);
	}


	public void TryReleasedAt(Vector3 PositionWorld)
	{
		if (CurrentlyStored <= 0)
		{
			return;
		}

		CurrentlyStored--;
		ObjectContainer.RetrieveObject(PositionWorld);
	}
}
