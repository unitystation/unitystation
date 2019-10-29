using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class StunBaton : NetworkBehaviour, IInteractable<HandActivate>
{
	public SpriteRenderer spriteRenderer;

	/// <summary>
	/// Sound played when turning this baton on/off
	/// </summary>
	[SerializeField]
	private string soundToggle;

	/// <summary>
	/// Sprite to be shown when the baton is on
	/// </summary>
	[SerializeField]
	private Sprite spriteActive;

	/// <summary>
	/// Sprite to be shown when the baton is off
	/// </summary>
	[SerializeField]
	private Sprite spriteInactive;

	[SyncVar(hook = nameof(UpdateState))]
	public bool active;

	public bool isActive => active;

	public void ToggleState()
	{
		UpdateState(!active);
	}

	private void UpdateState(bool newState)
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

		if (UIManager.Hands.CurrentSlot != null)
		{
			// UIManager doesn't update held item sprites automatically
			if (UIManager.Hands.CurrentSlot.Item == gameObject)
			{
				UIManager.Hands.CurrentSlot.UpdateImage(gameObject);
			}
		}
	}

	public void ClientPredictInteraction(HandActivate interaction)
	{
		ToggleState();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		SoundManager.PlayNetworkedAtPos(soundToggle, interaction.Performer.transform.position);
		ToggleState();
	}
}