using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Player;
using UnityEngine;

public class SetBodyType : MonoBehaviour
{
	public BodyType ToSetTo;

	public Pickupable Pickupable;

	public void Awake()
	{
		Pickupable = this.GetComponent<Pickupable>();
	}

	public void Start()
	{
		if (Pickupable.ItemSlot?.Player != null)
		{
			var PlayerSprites = Pickupable.ItemSlot?.Player.GetComponent<PlayerSprites>();
			PlayerSprites.SetAllBodyType(ToSetTo);
		}
	}
}
