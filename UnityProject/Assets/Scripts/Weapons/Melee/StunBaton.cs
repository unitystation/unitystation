using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class StunBaton : NetworkBehaviour, IPredictedInteractable<HandActivate>
{
	public SpriteRenderer spriteRenderer;

	/// <summary>
	/// Sound played when turning this baton on/off
	/// </summary>
	[SerializeField]
	private string soundToggle = null;

	/// <summary>
	/// Sprite to be shown when the baton is on
	/// </summary>
	[SerializeField]
	private Sprite spriteActive = null;

	/// <summary>
	/// Sprite to be shown when the baton is off
	/// </summary>
	[SerializeField]
	private Sprite spriteInactive = null;

	[SyncVar(hook = nameof(UpdateState))]
	public bool active;

	public bool isActive => active;

	private Pickupable pickupable;

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
	}

	public void ToggleState()
	{
		UpdateState(active, !active);
	}

	private void UpdateState(bool oldValue, bool newState)
	{
		active = newState;

		UpdateSprite();
	}

	private void UpdateSprite()
	{
		if (active)
		{
			spriteRenderer.sprite = spriteActive;
		}
		else
		{
			spriteRenderer.sprite = spriteInactive;
		}

		Inventory.RefreshUISlotImage(gameObject);
	}

	public void ClientPredictInteraction(HandActivate interaction)
	{
		ToggleState();
	}

	public void ServerRollbackClient(HandActivate interaction)
	{
		UpdateState(active, active);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		SoundManager.PlayNetworkedAtPos(soundToggle, interaction.Performer.transform.position, sourceObj: interaction.Performer);
		ToggleState();
	}
}