using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Allows object to function as a door switch - opening / closing door when clicked.
/// </summary>
public class DoorSwitch : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private SpriteRenderer spriteRenderer;
	public Sprite onSprite;
	public Sprite offSprite;

	public DoorController[] doorControllers;

	private bool buttonCoolDown = false;

	private void Start()
	{
		//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
		gameObject.layer = LayerMask.NameToLayer("WallMounts");
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//this validation is only done client side for their convenience - they can't
		//press button while it's animating.
		if (side == NetworkSide.Client)
		{
			if (buttonCoolDown) return false;
			buttonCoolDown = true;
			StartCoroutine(CoolDown());
		}

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		for (int i = 0; i < doorControllers.Length; i++)
		{
			if (!doorControllers[i].IsOpened)
			{
				doorControllers[i].ServerOpen();
			}
			else
			{
				doorControllers[i].ServerClose();
			}
		}

		RpcPlayButtonAnim();
	}

	//Stops spamming from players
	IEnumerator CoolDown()
	{
		yield return WaitFor.Seconds(1.2f);
		buttonCoolDown = false;
	}

	[ClientRpc]
	public void RpcPlayButtonAnim()
	{
		StartCoroutine(ButtonFlashAnim());
	}

	IEnumerator ButtonFlashAnim()
	{
		if (spriteRenderer == null)
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}

		for (int i = 0; i < 6; i++)
		{
			if (spriteRenderer.sprite == onSprite)
			{
				spriteRenderer.sprite = offSprite;
			}
			else
			{
				spriteRenderer.sprite = onSprite;
			}

			yield return WaitFor.Seconds(0.2f);
		}
	}
}