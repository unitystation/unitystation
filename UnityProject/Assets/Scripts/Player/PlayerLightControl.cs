using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using UnityEngine.Networking;

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

[RequireComponent(typeof(Pickupable))]
public class PlayerLightControl : NetworkBehaviour
{
	public LightEmissionPlayer LightEmission;

	public HashSet<EquipSlot> CompatibleSlots = new HashSet<EquipSlot>() {
		EquipSlot.leftHand,
		EquipSlot.rightHand,
		EquipSlot.suitStorage,
		EquipSlot.belt,
		EquipSlot.back,
		EquipSlot.storage01,
		EquipSlot.storage02,
		EquipSlot.suitStorage
	};

	public float Intensity;
	public Color Colour;
	//public Sprite Sprite;
	public EnumSpriteLightData EnumSprite;
	public float Size;

	public PlayerLightData PlayerLightData;

	private void OnPickupServer(HandApply interaction)
	{
		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();
		InventorySlot Slot = InventoryManager.GetSlotFromItem(gameObject, pna);
		if (Slot != null)
		{
			LightEmission = Slot.Owner.GetComponent<LightEmissionPlayer>();
			LightEmission.AddLight(PlayerLightData);
		}
	}
	private void OnDrop()
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

		var pickup = GetComponent<Pickupable>();
		if (pickup != null)
		{
			pickup.OnPickupServer.AddListener(OnPickupServer);
			pickup.OnDropServer.AddListener(OnDrop);
		}
	}
	public void OnAddToInventorySlot(InventorySlot slot)
	{
		if (isServer)
		{
			if (slot.IsUISlot)
			{
				if (!(CompatibleSlots.Contains(slot.equipSlot)))
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
}
