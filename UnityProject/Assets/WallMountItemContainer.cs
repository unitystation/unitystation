using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WallMountItemContainer : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	// Start is called before the first frame update

	public ItemTrait traitRequired;
	public GameObject appliableItem;

	public Sprite[] spriteListBroken;
	public Sprite[] spriteListEmpty;
	public Sprite[] spriteListFull;
	public Sprite[] spriteListLightOn;

	public SpriteRenderer spriteRenderer;
	public SpriteRenderer spriteRendererLightOn;

	private Orientation orientation;
	bool hasItem = true;

	//private ItemStorage itemStorage;
	//private ItemSlot itemSlot;

	private void Awake()
	{
		orientation = GetComponent<Directional>().CurrentDirection;
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
			Chat.AddGameWideSystemMsgToChat(orientation.Degrees.ToString());
			spriteRenderer.sprite = GetSprite(spriteListEmpty);
			spriteRendererLightOn.sprite = null;
			Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
			//Inventory.ServerTransfer(itemSlot, interaction.HandSlot);
			Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
			hasItem = false;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, traitRequired) && !hasItem)
		{
			spriteRenderer.sprite = GetSprite(spriteListFull);
			spriteRendererLightOn.sprite = GetSprite(spriteListLightOn);
			//Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
			Despawn.ServerSingle(interaction.HandObject);
			Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
			hasItem = true;
		}
	}

	private Sprite GetSprite(Sprite[] spriteList)
	{
		int angle = orientation.Degrees;
		switch(angle)
		{
			case 0:
				return spriteList[1];
			case 90:
				return spriteList[0];
			case 180:
				return spriteList[3];
			default:
				return spriteList[2];
		}
	}

	// Update is called once per frame
	void Update()
	{

	}
}