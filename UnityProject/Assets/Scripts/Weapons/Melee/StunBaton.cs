using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Pickupable))]
public class StunBaton : InputTrigger
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

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		return false;
	}

	public override void UI_Interact(GameObject originator, string hand)
	{
		if (!isServer)
		{
			UIInteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
		}
		else
		{
			SoundManager.PlayNetworkedAtPos(soundToggle, originator.transform.position);
			ToggleState();
		}
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
