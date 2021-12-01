using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupermatterSliverContainer : MonoBehaviour, ICheckedInteractable<HandActivate>
{
	private const int NOT_LOADED_SPRITE = 0;
	private const int LOADED_SPRITE = 1;
	private const int SEALED_SPRITE = 2;

	public SpriteHandler[] spriteHandlers = new SpriteHandler[] { };
	private ItemStorage itemStorage;
	private ItemSlot sliverSlot;
	public bool isSealed = false;
	public bool isLoaded = false;

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
		sliverSlot = itemStorage.GetIndexedItemSlot(0);
		sliverSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
	}

	private void OnServerSlotContentsChange()
	{
		isLoaded = sliverSlot.ItemObject == null ? false : true;
		updateSprites();
	}

	public bool LoadSliver(ItemSlot fromSlot)
	{
		bool result = Inventory.ServerTransfer(fromSlot, sliverSlot);
		return result;
	}

	public bool UnloadSliver()
	{
		bool result = Inventory.ServerDrop(sliverSlot);
		return result;
	}

	private void updateSprites()
	{
		foreach (var handler in spriteHandlers)
		{
			if (handler)
			{
				int newSpriteID;
				if(!isLoaded)
				{
					newSpriteID = NOT_LOADED_SPRITE;
				}
				else if(isLoaded && !isSealed)
				{
					newSpriteID = LOADED_SPRITE;
				}
				else
				{
					newSpriteID = SEALED_SPRITE;
				}
				handler.ChangeSprite(newSpriteID);
			}
		}
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (isSealed) return false;

		if (!isLoaded) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, $"You seal the container.");
		isSealed = true;
		updateSprites();
	}
}
