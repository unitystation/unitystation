using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Pickupable))]
public class StunBaton : NetworkBehaviour
{
	public SpriteRenderer spriteRenderer;

	public string soundToggle;

	public Sprite spriteActive;
	public Sprite spriteInactive;

	[SyncVar(hook = nameof(UpdateState))]
	private bool active;

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

		if (UIManager.Hands.CurrentSlot.Item == gameObject)
		{
			UIManager.Hands.CurrentSlot.UpdateImage(gameObject);
		}
	}

	public bool isActive()
	{
		return active;
	}
}
