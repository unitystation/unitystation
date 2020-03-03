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

	[Header("Access Restrictions for ID")]
	[Tooltip("Is this door restricted?")] public bool restricted;
	[Tooltip("Access level to limit door if above is set.")] public Access access;


	public DoorController[] doorControllers;

	private bool buttonCoolDown = false;
	private AccessRestrictions accessRestrictions;

	private void Start()
	{
		//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
		gameObject.layer = LayerMask.NameToLayer("WallMounts");
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		accessRestrictions = gameObject.AddComponent<AccessRestrictions>();
		if (restricted)
		{
			accessRestrictions.restriction = access;
		}
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
		if (accessRestrictions != null && restricted)
		{
			if (accessRestrictions.CheckAccess(interaction.Performer))
			{
				RunDoorController();
				RpcPlayButtonAnim(true);
			}
			else
			{
				RpcPlayButtonAnim(false);
			}
		}
		else
		{
			RunDoorController();
			RpcPlayButtonAnim(true);
		}
	}

	private void RunDoorController()
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
	}

	//Stops spamming from players
	IEnumerator CoolDown()
	{
		yield return WaitFor.Seconds(1.2f);
		buttonCoolDown = false;
	}

	[ClientRpc]
	public void RpcPlayButtonAnim(bool status)
	{
		StartCoroutine(ButtonFlashAnim(status));
	}

	IEnumerator ButtonFlashAnim(bool status)
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
	}

	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled doors with red lines and spheres
		Gizmos.color = new Color(1, 0, 0, 1);
		for (int i = 0; i < doorControllers.Length; i++)
		{
			var doorController = doorControllers[i];
			Gizmos.DrawLine(sprite.transform.position, doorController.transform.position);
			Gizmos.DrawSphere(doorController.transform.position, 0.25f);
		}
	}
}