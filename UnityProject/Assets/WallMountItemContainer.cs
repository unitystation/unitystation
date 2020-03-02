using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WallMountItemContainer : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	// Start is called before the first frame update

	public ItemTrait traitRequired;
	public GameObject appliableItem;

	bool hasItem = true;

	//private ItemStorage itemStorage;
	//private ItemSlot itemSlot;

	private void Awake()
	{
		//itemStorage = GetComponent<ItemStorage>();
		//itemSlot = itemStorage.GetIndexedItemSlot(0);
	}
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.HandObject == null && hasItem)
		{
			//take out mountedItem
			//Inventory.ServerAdd(appliableItem, interaction.HandSlot);
			Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
			//Inventory.ServerTransfer(itemSlot, interaction.HandSlot);
			Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
			hasItem = false;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, traitRequired) && !hasItem)
		{
			hasItem = true;
			//Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
			Despawn.ServerSingle(interaction.HandObject);
			Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
		}
	}

	// Update is called once per frame
	void Update()
	{

	}
}