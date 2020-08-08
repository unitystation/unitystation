using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Base class for smokable cigarette
/// </summary>
public class Cigarette : NetworkBehaviour, ICheckedInteractable<HandApply>,
	ICheckedInteractable<InventoryApply>, IServerDespawn
{
	private const int DEFAULT_SPRITE = 0;
	private const int LIT_SPRITE = 1;

	[SerializeField]
	private GameObject buttPrefab;
	[SerializeField]
	private float smokeTimeSeconds = 180;

	public SpriteHandler spriteHandler;
	private FireSource fireSource;
	private Pickupable pickupable;

	[SyncVar]
	private bool isLit;

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		fireSource = GetComponent<FireSource>();
	}

	private void Update()
	{
		if (isClient)
		{
			// update UI image on client (cigarette lit animation)
			if (isLit)
			{
				pickupable?.RefreshUISlotImage();
			}
		}
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		TryLightByObject(interaction.UsedObject);
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		TryLightByObject(interaction.UsedObject);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}
		if (interaction.UsedObject == null)
		{
			return false;
		}

		return true;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}
		if (interaction.UsedObject == null)
		{
			return false;
		}

		return true;
	}

	private void ServerChangeLit(bool isLitNow)
	{
		// update cigarette sprite to lit state
		if (spriteHandler)
		{
			var newSpriteID = isLitNow ? LIT_SPRITE : DEFAULT_SPRITE;
			spriteHandler.ChangeSprite(newSpriteID, false);
		}

		// toggle flame from cigarette
		if (fireSource)
		{
			fireSource.IsBurning = isLitNow;
		}

		isLit = isLitNow;
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		ServerChangeLit(false);
	}

	private bool TryLightByObject(GameObject usedObject)
	{
		if (!isLit)
		{
			// check if player tries to lit cigarette with something
			if (usedObject != null)
			{
				// check if it's something like lighter or candle
				var fireSource = usedObject.GetComponent<FireSource>();
				if (fireSource && fireSource.IsBurning)
				{
					ServerChangeLit(true);
					return true;
				}
			}
		}

		return false;
	}
}
