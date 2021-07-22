using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;
using Mirror;
using UI.Action;

public class ItemMagBoots : NetworkBehaviour, IServerInventoryMove, PlayerMove.IMovementEffect
{
	[Tooltip("The speed debuff to apply to run speed.")]
	[SerializeField]
	private float runSpeedDebuff = -1.5f;

	private SpriteHandler spriteHandler;
	private ItemAttributesV2 itemAttributesV2;
	private Pickupable pickupable;
	private PlayerMove playerMove;
	private ItemActionButton actionButton;

	private bool isOn = false;

	public float RunningAdd {
		get => runSpeedDebuff;
		set { }
	}

	public float WalkingAdd {
		get => 0;
		set { }
	}


	public float CrawlAdd
	{
		get => 0;
		set { }
	}




	private enum SpriteState
	{
		Off = 0,
		On = 1
	}

	#region Lifecycle

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		itemAttributesV2 = GetComponent<ItemAttributesV2>();
		actionButton = GetComponent<ItemActionButton>();
		pickupable.RefreshUISlotImage();
	}

	private void OnEnable()
	{
		actionButton.ServerActionClicked += ToggleState;
	}

	private void OnDisable()
	{
		actionButton.ServerActionClicked -= ToggleState;
	}

	#endregion Lifecycle

	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (info.ToRootPlayer != null)
		{
			playerMove = info.ToRootPlayer.PlayerScript.PlayerMove;
		}
		else if (info.FromRootPlayer != null)
		{
			playerMove = info.FromRootPlayer.PlayerScript.PlayerMove;
		}

		if (isOn)
		{
			ToggleOff();
		}
	}

	private void ToggleState()
	{
		if (isOn)
		{
			ToggleOff();
		}
		else
		{
			ToggleOn();
		}
	}

	private void ToggleOn()
	{
		isOn = true;
		ApplyEffect();
		spriteHandler.ChangeSprite((int) SpriteState.On);
		pickupable.RefreshUISlotImage();
	}

	private void ToggleOff()
	{
		isOn = false;
		RemoveEffect();
		spriteHandler.ChangeSprite((int) SpriteState.Off);
		pickupable.RefreshUISlotImage();
	}

	private void ApplyEffect()
	{
		itemAttributesV2.AddTrait(CommonTraits.Instance.NoSlip);
		playerMove.AddModifier(this);
		playerMove.PlayerScript.PushPull.ServerSetPushable(false);
	}

	private void RemoveEffect()
	{
		itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
		playerMove.RemoveModifier(this);
		playerMove.PlayerScript.PushPull.ServerSetPushable(true);
	}
}
