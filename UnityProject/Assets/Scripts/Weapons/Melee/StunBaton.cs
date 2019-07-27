using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Pickupable))]
public class StunBaton : NBHandActivateInteractable
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
	private bool active;

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

		// UIManager doesn't update held item sprites automatically
		if (UIManager.Hands.CurrentSlot.Item == gameObject)
		{
			UIManager.Hands.CurrentSlot.UpdateImage(gameObject);
		}
	}

	protected override void ClientPredictInteraction(HandActivate interaction)
	{
		ToggleState();
	}

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		SoundManager.PlayNetworkedAtPos(soundToggle, interaction.Performer.transform.position);
		ToggleState();
	}
}
