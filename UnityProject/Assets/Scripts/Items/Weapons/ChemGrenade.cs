using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChemGrenade : MonoBehaviour, ICheckedInteractable<HandActivate>
{
	private const int UNSECURED_SPRITE = 0;
	private const int SECURED_SPRITE = 1;
	private const int ACTIVE_SPRITE = 2;

	public SpriteHandler[] spriteHandlers = new SpriteHandler[] { };

	private ItemStorage itemStorage;
	private ItemSlot containerA;
	private ItemSlot containerB;
	private ItemSlot detonator;

	private bool isSecured = false;
	private bool isActive = false;

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
		containerA = itemStorage.GetIndexedItemSlot(0);
		containerB = itemStorage.GetIndexedItemSlot(1);
		detonator = itemStorage.GetIndexedItemSlot(2);
	}

	private void UpdateSprites()
	{
		foreach (var handler in spriteHandlers)
		{
			if (handler)
			{
				int newSpriteID = isSecured ? SECURED_SPRITE : UNSECURED_SPRITE;
				newSpriteID = isActive ? ACTIVE_SPRITE : newSpriteID;
				handler.ChangeSprite(newSpriteID);
			}
		}
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false || isSecured == false || containerA.Item == null || containerB.Item == null || detonator.Item == null) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		isActive = true;
		Activate();
		UpdateSprites();
	}

	public void Activate()
    {

    }
}
