using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Allows object to function as a door switch - opening / closing door when clicked.
/// </summary>
public class DoorSwitch : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private SpriteRenderer spriteRenderer;
	public Sprite greenSprite;
	public Sprite offSprite;
	public Sprite redSprite;
	private bool status;


	public DoorController[] doorControllers;

	private bool buttonCoolDown = false;

	private AccessRestrictions accessRestrictions;
	public AccessRestrictions AccessRestrictions
	{
		get
		{
			if (!accessRestrictions)
			{
				accessRestrictions = GetComponent<AccessRestrictions>();
			}
			return accessRestrictions;
		}
	}

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
		if (AccessRestrictions != null)
		{
			if (accessRestrictions.CheckAccess(interaction.Performer))
			{
				status = true;
				RunDoorController();
				RpcPlayButtonAnim();
			}
			else
			{
				status = false;
				RpcPlayButtonAnim();
			}
		}
		else
		{
			status = true;
			RunDoorController();
			RpcPlayButtonAnim();
		}
	}

	private void RunDoorController()
	{
		for (int i = 0; i < doorControllers.Length; i++)
		{
			if (!doorControllers[i].IsOpened)
			{
				doorControllers[i].Open();
			}
			else
			{
				doorControllers[i].Close();
			}
		}
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
			if (status)
			{
				if (spriteRenderer.sprite == greenSprite)
				{
					spriteRenderer.sprite = offSprite;
				}
				else
				{
					spriteRenderer.sprite = greenSprite;
				}
				yield return WaitFor.Seconds(0.2f);
			}
			else
			{
				if (spriteRenderer.sprite == redSprite)
				{
					spriteRenderer.sprite = offSprite;
				}
				else
				{
					spriteRenderer.sprite = redSprite;
				}
				yield return WaitFor.Seconds(0.1f);
			}

		}
		spriteRenderer.sprite = greenSprite;
		status = false;
	}
}