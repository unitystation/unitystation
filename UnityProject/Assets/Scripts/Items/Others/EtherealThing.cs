using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

public class EtherealThing : MonoBehaviour, IServerSpawn
{

	public Pickupable Pickupable;


	private bool InIted = false;

	public void OnSpawnServer(SpawnInfo info)
	{
		Pickupable = this.GetComponent<Pickupable>();
		if (this.GetComponent<RuntimeSpawned>() == null)
		{
			StartCoroutine(WaitingFrame());
		}
		else
		{
			//EtherealThing TODO
			//Destroy(this.gameObject);
			StartCoroutine(WaitingFrame());
		}
	}

	public void Start()
	{
		if (this.GetComponent<RuntimeSpawned>() == null)
		{
			StartCoroutine(WaitingFrame());
		}
	}

	private IEnumerator WaitingFrame()
	{
		if (InIted)
		{
			yield break;
		}

		InIted = true;
		yield return null;


		if (CustomNetworkManager.IsServer)
		{
			if (Pickupable != null && Pickupable.ItemSlot != null)
			{
				Inventory.ServerDrop(Pickupable.ItemSlot); //TOOD Handle inventory sometime
			}
		}

		var RegisterTile = this.GetComponent<RegisterTile>();
		RegisterTile.Matrix.MetaDataLayer.EtherealThings.Add(this);
	}
}
