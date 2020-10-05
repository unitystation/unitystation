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
	[Tooltip("Object to spawn after cigarette burnt")]
	private GameObject buttPrefab = null;

	[SerializeField]
	[Tooltip("Time after cigarette will destroy and spawn butt")]
	private float smokeTimeSeconds = 180;

	public SpriteHandler spriteHandler = null;
	private FireSource fireSource = null;
	private Pickupable pickupable = null;

	[SyncVar]
	private bool isLit = false;

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		fireSource = GetComponent<FireSource>();
	}

	#region Interactions
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
		// standard validation for interaction
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		return CheckInteraction(interaction, side);
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		// standard validation for interaction
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		return CheckInteraction(interaction, side);
	}

	private bool CheckInteraction(Interaction interaction, NetworkSide side)
	{
		// check if player want to use some light-source
		if (interaction.UsedObject)
		{
			var lightSource = interaction.UsedObject.GetComponent<FireSource>();
			if (lightSource)
			{
				return true;
			}
		}

		return false;
	}
	#endregion

	private void ServerChangeLit(bool isLitNow)
	{
		// TODO: add support for in-hand and clothing animation
		// update cigarette sprite to lit state
		if (spriteHandler)
		{
			var newSpriteID = isLitNow ? LIT_SPRITE : DEFAULT_SPRITE;
			spriteHandler.ChangeSprite(newSpriteID);
		}

		// toggle flame from cigarette
		if (fireSource)
		{
			fireSource.IsBurning = isLitNow;
		}

		if (isLitNow)
		{
			StartCoroutine(FireRoutine());
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

	private void Burn()
	{
		var worldPos = gameObject.AssumedWorldPosServer();
		var tr = gameObject.transform.parent;
		var rotation = RandomUtils.RandomRotatation2D();

		// Print burn out message if in players inventory
		if (pickupable && pickupable.ItemSlot != null)
		{
			var player = pickupable.ItemSlot.Player;
			if (player)
			{
				Chat.AddExamineMsgFromServer(player.gameObject,
					$"Your {gameObject.ExpensiveName()} goes out.");
			}
		}

		// Despawn cigarette
		Despawn.ServerSingle(gameObject);
		// Spawn cigarette butt
		Spawn.ServerPrefab(buttPrefab, worldPos, tr, rotation);
	}

	private IEnumerator FireRoutine()
	{
		// wait until cigarette will burn
		yield return new WaitForSeconds(smokeTimeSeconds);
		// despawn cigarette and spawn burn
		Burn();
	}
}
