using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using System.Linq;

public class ItemMagBoots : NetworkBehaviour, IInteractable<HandActivate>
{
	private bool isOn = false;
	private GameObject player;
	private ItemAttributesV2 itemAttributesV2;
	private void Awake()
	{
		itemAttributesV2 = GetComponent<ItemAttributesV2>();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		player = interaction.Performer;
		isOn = !isOn;
		if (isOn)
		{
			player.GetComponent<PlayerMove>().RunSpeed = 1;
			player.GetComponent<PlayerMove>().WalkSpeed = 1;
			player.GetComponent<PlayerSync>().SpeedServer  = 1;
			Debug.Log("Speed is 4 ");
			itemAttributesV2.AddTrait(CommonTraits.Instance.NoSlip);
		}
		else
		{
			player.GetComponent<PlayerMove>().RunSpeed = 20;
			player.GetComponent<PlayerMove>().WalkSpeed = 20;
			player.GetComponent<PlayerSync>().SpeedServer  = 20;
			Debug.Log("Speed is 6 ");
			itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
		}
		player.GetComponent<ObjectBehaviour>().ServerSetPushable(!isOn);
		Debug.Log("MagBoots are " + isOn.ToString());
	}

	private void OnDestroy()
	{
		if (player != null && !player.GetComponent<ObjectBehaviour>().IsPushable)
		{
			player.GetComponent<ObjectBehaviour>().ServerSetPushable(false);
		}
	}
}
