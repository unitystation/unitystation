using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Allows object to function as a door switch - opening / closing door when clicked.
/// </summary>
public class DoorSwitch : NBHandApplyInteractable
{
	private Animator animator;
	private SpriteRenderer spriteRenderer;

	public DoorController[] doorControllers;

	private void Start()
	{
		//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
		gameObject.layer = LayerMask.NameToLayer("WallMounts");
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		animator = GetComponent<Animator>();
	}

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS
			.WithValidation(TargetIs.GameObject(gameObject))
			.WithValidation(ClientSwitchValidation);
	}

	private ValidationResult ClientSwitchValidation(HandApply interaction, NetworkSide side)
	{
		//this validation is only done client side for their convenience - they can't
		//press button while it's animating.
		if (side == NetworkSide.CLIENT)
		{
			//if the button is idle and not animating it can be pressed
			if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
			{
				return ValidationResult.SUCCESS;
			}
		}

		return ValidationResult.FAIL;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		for (int i = 0; i < doorControllers.Length; i++)
		{
			if (!doorControllers[i].IsOpened)
			{
				doorControllers[i].Open();
			}
		}
		RpcPlayButtonAnim();
	}

	[ClientRpc]
	public void RpcPlayButtonAnim()
	{
		animator.SetTrigger("activated");
	}
}