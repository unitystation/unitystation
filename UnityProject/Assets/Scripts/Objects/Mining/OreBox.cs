using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class OreBox : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerDespawn
{
	private ItemStorage oreBoxItemStorage;
	private RegisterTile registerTile;
	public GameObject matsOnDestroy;
	private void Awake()
	{
		oreBoxItemStorage = GetComponent<ItemStorage>();
		registerTile = GetComponent<RegisterTile>();
	}

	private void OnEnable()
	{
		registerTile.OnLocalPositionChangedServer.AddListener(AfterMovement);
	}

	private void OnDisable()
	{
		registerTile.OnLocalPositionChangedServer.RemoveListener(AfterMovement);
	}

	private void AfterMovement(Vector3Int newLocalPosition)
	{
		if (isServer == false) return;
		var tileObjects = MatrixManager.GetAt<UniversalObjectPhysics>(registerTile.WorldPosition, true);
		foreach (var objectBehaviour in tileObjects)
		{
			var item = objectBehaviour.gameObject;
			if (Validations.HasItemTrait(item, CommonTraits.Instance.OreGeneral))
			{
				var oreBoxSlot = oreBoxItemStorage.GetBestSlotFor(item);
				Inventory.ServerAdd(item, oreBoxSlot);
			}
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;
		if (interaction.HandSlot.IsEmpty && interaction.IsAltClick)
			return true;
		if (Validations.HasItemTrait(interaction.HandSlot.ItemObject, CommonTraits.Instance.Crowbar))
			return true;
		if (Validations.HasItemTrait(interaction.HandSlot.ItemObject, CommonTraits.Instance.OreGeneral))
			return true;
		if (interaction.HandSlot?.ItemObject.OrNull()?.GetComponent<ItemStorage>())
			return true;

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var itemObject = interaction.HandSlot.ItemObject;
		if (Validations.HasItemTrait(itemObject, CommonTraits.Instance.Crowbar) || (interaction.HandSlot.ItemObject == null && interaction.IsAltClick))
		{
			Chat.AddActionMsgToChat(interaction.Performer.gameObject, $"You empty out the {this.gameObject.ExpensiveName()} Quickly", $" {interaction.Performer} empties out the {this.gameObject.ExpensiveName()} Quickly");
			oreBoxItemStorage.ServerDropAllAtWorld(interaction.Performer.AssumedWorldPosServer());
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
