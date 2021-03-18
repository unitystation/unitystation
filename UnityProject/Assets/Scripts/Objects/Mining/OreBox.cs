using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class OreBox : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerDespawn
{
	private ItemStorage oreBoxItemStorage;
	public GameObject matsOnDestroy;
	private void Awake()
	{
		oreBoxItemStorage = GetComponent<ItemStorage>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;
		if (interaction.HandSlot.IsEmpty)
			return false;
		if (Validations.HasItemTrait(interaction.HandSlot.ItemObject, CommonTraits.Instance.Crowbar))
			return true;
		if (Validations.HasItemTrait(interaction.HandSlot.ItemObject, CommonTraits.Instance.OreGeneral))
			return true;
		if (interaction.HandSlot.ItemObject.GetComponent<ItemStorage>())
			return true;

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var itemObject = interaction.HandSlot.ItemObject;
		if (Validations.HasItemTrait(itemObject, CommonTraits.Instance.Crowbar))
		{
			oreBoxItemStorage.ServerDropAll();
		}
		else if (Validations.HasItemTrait(itemObject, CommonTraits.Instance.OreGeneral))
		{
			TransferOre(interaction.HandSlot);
		}
		else
		{
			var itemStorage = itemObject.GetComponent<ItemStorage>();
			var itemSlotList = itemStorage.GetItemSlots();
			foreach (var itemSlot in itemSlotList)
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				TransferOre(itemSlot);
			}
		}
	}

	private void TransferOre(ItemSlot itemSlot)
	{
		var oreBoxSlot = oreBoxItemStorage.GetBestSlotFor(itemSlot.Item);
		if (oreBoxSlot != null)
		{
			Inventory.ServerTransfer(itemSlot, oreBoxSlot);
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		Spawn.ServerPrefab(matsOnDestroy, gameObject.TileWorldPosition().To3Int(), transform.parent, count: 4,
			scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
	}

}
