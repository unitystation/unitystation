using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;

/// <summary>
/// The 'fancy' component defines objects like donut boxes, egg boxes or cigs packets
/// that show how many items are in the ItemStorage on the sprite itself
///
/// Need to have ItemStorage component on a same gameobject
/// </summary>
[RequireComponent(typeof(ItemStorage))]
public class FancyItemStorage : NetworkBehaviour
{
	public SpriteHandler spriteHandler;
	[Tooltip("Sprite for each objects count inside container")]
	public Sprite[] spritesByCount;

	private ItemStorage storage;
	private Pickupable pickupable;

	[SyncVar(hook = "OnObjectsCountChanged")]
	private int objectsInsideCount;

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
	}

	[ServerCallback]
	private void Start()
	{
		// get ItemStorage on a same object
		storage = GetComponent<ItemStorage>();
		if (!storage)
		{
			Loggy.LogError($"FancyItemStorage on {gameObject.name} can't find ItemStorage component!", Category.Inventory);
			return;
		}

		// get all slots and subscribe if any slot changed
		var allSlots = storage.GetItemSlots();
		foreach (var slot in allSlots)
		{
			slot.OnSlotContentsChangeServer.AddListener(OnStorageChanged);
		}

		// calculate occupied slots count
		objectsInsideCount = allSlots.Count((s) => s.IsOccupied);
	}

	private void OnObjectsCountChanged(int oldVal, int objectsCount)
	{
		if (!spriteHandler || spritesByCount == null
			|| spritesByCount.Length == 0)
		{
			return;
		}

		// select new sprite for container
		Sprite newSprite;
		if (objectsCount < spritesByCount.Length)
		{
			newSprite = spritesByCount[objectsCount];
		}
		else
		{
			newSprite = spritesByCount.Last();
		}

		if (newSprite)
		{
			// apply it to sprite handler
			spriteHandler.SetSprite(newSprite);
			// try to update in-hand sprite
			pickupable?.RefreshUISlotImage();
		}
	}

	[Server]
	private void OnStorageChanged()
	{
		// recalculate occupied slots count and send to client
		var allSlots = storage.GetItemSlots();
		objectsInsideCount = allSlots.Count((s) => s.IsOccupied);
	}
}
