using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

[Serializable]
public class PlayerLightData
{
	public float Intensity = 0.0f;
	public Color Colour;
	//todo Make it so badmins can Mess around with the sprite so It can be set to anything they desire
	//public Sprite Sprite;
	public EnumSpriteLightData EnumSprite;
	public float Size = 12;
}

public enum EnumSpriteLightData
{
	Default,
	Square,
	Clown,
}

public class PlayerLightControl : PickUpTrigger
{
	public LightEmissionPlayer LightEmission;

	public HashSet<string> CompatibleSlots = new HashSet<string>() {
		"leftHand",
		"rightHand",
		"suitStorage",
		"belt",
		"back",
		"storage01",
		"storage02",
		"suitStorage",
	};

	public float Intensity;
	public Color Colour;
	//public Sprite Sprite;
	public EnumSpriteLightData EnumSprite;
	public float Size;

	public PlayerLightData PlayerLightData;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		bool OnPickUp = false;
		if (gameObject != UIManager.Hands.CurrentSlot.Item)
		{
			OnPickUp = true;

		}
		bool Store = base.Interact(originator, position, hand);
		if (OnPickUp)
		{
			OnPickup();
		}
		return (Store);
	}
	public override void OnDropItemServer()
	{
		OnDrop();
		base.OnDropItemServer();
	}
	public void OnPickup()
	{
		InventorySlot Slot = InventoryManager.GetSlotFromItem(this.gameObject);
		if (Slot != null)
		{
			LightEmission = Slot.Owner.gameObject.GetComponent<LightEmissionPlayer>();
			LightEmission.AddLight(PlayerLightData);
		}
	}
	public void OnDrop()
	{
		if (LightEmission != null)
		{
			LightEmission.RemoveLight(PlayerLightData);
			LightEmission = null;
		}
	}
	void Start()
	{
		PlayerLightData = new PlayerLightData()
		{
			Intensity = Intensity,
			Colour = Colour,
			EnumSprite = EnumSprite,
			Size = Size,
		};
	}
	public void OnAddToInventorySlot(InventorySlot slot)
	{
		if (slot.IsUISlot)
		{
			if (!(CompatibleSlots.Contains(slot.SlotName)))
			{
				LightEmission.RemoveLight(PlayerLightData);
			}
			else
			{
				if (LightEmission != null)
				{
					LightEmission.AddLight(PlayerLightData);
				}
			}
		}
		else
		{
			LightEmission.RemoveLight(PlayerLightData);
		}
	}
}
