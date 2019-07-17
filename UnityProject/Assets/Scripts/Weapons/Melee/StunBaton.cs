using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(StunBatonActivate))]
public class StunBaton : NetworkBehaviour
{
	public SpriteRenderer spriteRenderer;

	/// <summary>
	/// Sound played when turning this baton on/off
	/// </summary>
	public string soundToggle;

	/// <summary>
	/// Sprite to be shown when the baton is on
	/// </summary>
	public Sprite spriteActive;

	/// <summary>
	/// Sprite to be shown when the baton is off
	/// </summary>
	public Sprite spriteInactive;

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
}
