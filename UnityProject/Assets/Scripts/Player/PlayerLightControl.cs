using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLightData  {
	public float Intensity = 0.0f;
	public Color Colour = new Color(0.7264151f, 0.7264151f, 0.7264151f, 0.8f);
	public Sprite Sprite;
	public float Size = 12;
	public GameObject Item;
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
	public Sprite Sprite;
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
		if (OnPickUp){
			OnPickup();
		}
	return (Store);
	}
	public override void OnDropItemServer()
	{
		OnDrop();
		base.OnDropItemServer();
	}
	public void OnPickup() {
		InventorySlot Slot = InventoryManager.GetSlotFromItem(this.gameObject);
		LightEmission =  Slot.Owner.gameObject.GetComponent<LightEmissionPlayer>();
		LightEmission.AddLight(PlayerLightData);
	}
	public void OnDrop()
	{
		LightEmission.RemoveLight(PlayerLightData);
		LightEmission = null;
	}
    void Start()
    {
		PlayerLightData = new PlayerLightData()
		{
			Intensity = Intensity,
			Colour = Colour,
			Sprite = Sprite,
			Size = Size,
			};
    }
	public void OnAddToInventorySlot(InventorySlot slot) {
		if (slot.IsUISlot)
		{
			if (!(CompatibleSlots.Contains(slot.SlotName)))
			{
				LightEmission.RemoveLight(PlayerLightData);
			}
			else {
				if (LightEmission != null) { 
					LightEmission.AddLight(PlayerLightData);
				}

			}
		}
		else { 
			LightEmission.RemoveLight(PlayerLightData);
		}
	}
    // Update is called once per frame
    void Update()
    {
        
    }
}
