using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Atmospherics;

public class ItemMagBoots : NetworkBehaviour,
	IServerActionGUI, IClientInventoryMove, IServerInventoryMove
{
	[Tooltip("The speed debuff to apply to run speed.")]
	[SerializeField]
	private float runSpeedDebuff = 1.5f;

	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	private SpriteHandler spriteHandler;
	private ConnectedPlayer player;
	private ItemAttributesV2 itemAttributesV2;
	private Pickupable pickupable;
	private PlayerMove playerMove;

	private bool isOn = false;

	private enum SpriteState
	{
		Off = 0,
		On = 1
	}

	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		itemAttributesV2 = GetComponent<ItemAttributesV2>();
		pickupable.RefreshUISlotImage();
	}

	private void ToggleState()
	{
		isOn = !isOn;
		spriteHandler.ChangeSprite(isOn ? (int) SpriteState.On : (int) SpriteState.Off);
		UIActionManager.SetSpriteSO(this, spriteHandler.GetCurrentSpriteSO());
		pickupable.RefreshUISlotImage();
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (IsPuttingOn(info))
		{
			playerMove = info.ToRootPlayer.PlayerScript.playerMove;
		}

		else if (IsTakingOff(info) & isOn)
		{
			playerMove = info.FromRootPlayer.PlayerScript.playerMove;
			ToggleState();
			RemoveEffect();
		}
	}

	private static bool IsPuttingOn (InventoryMove info)
	{
		if (info.ToSlot?.NamedSlot == null)
		{
			return false;
		}
		return info.ToSlot.NamedSlot == NamedSlot.feet;
	}

	private static bool IsTakingOff (InventoryMove info)
	{
		if (info.FromSlot?.NamedSlot == null)
		{
			return false;
		}
		return info.FromSlot.NamedSlot == NamedSlot.feet;
	}

	private void ApplyEffect()
	{
		itemAttributesV2.AddTrait(CommonTraits.Instance.NoSlip);
		playerMove.ServerChangeSpeed(playerMove.RunSpeed - runSpeedDebuff, playerMove.WalkSpeed);
		playerMove.PlayerScript.pushPull.ServerSetPushable(false);
	}

	private void RemoveEffect()
	{
		itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
		playerMove.ServerChangeSpeed(playerMove.RunSpeed + runSpeedDebuff, playerMove.WalkSpeed);
		playerMove.PlayerScript.pushPull.ServerSetPushable(true);
	}

	[Server]
	public void ServerChangeState(ConnectedPlayer newPlayer)
	{
		ToggleState();
		player = newPlayer;
		if (!ValidPlayer()) return;
		if (isOn)
		{
			ApplyEffect();
			player.Script.playerHealth.OnDeathNotifyEvent += OnPlayerDeath;
		}
		else
		{
			RemoveEffect();
			player.Script.playerHealth.OnDeathNotifyEvent -= OnPlayerDeath;
		}
	}

	private void OnPlayerDeath()
	{
		if (!ValidPlayer()) return;

		if (isServer)
		{
			if (isOn)
			{
				ServerChangeState(player);
			}
			UIActionManager.ToggleLocal(this, false);
			player.Script.playerHealth.OnDeathNotifyEvent -= OnPlayerDeath;
			player = null;
		}
		else
		{
			UIActionManager.ToggleLocal(this, false);
		}
	}

	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		if (CustomNetworkManager.Instance._isServer && GameData.IsHeadlessServer)
			return;
		var pna = PlayerManager.LocalPlayerScript.playerNetworkActions;
		switch (info.ClientInventoryMoveType)
		{
			case ClientInventoryMoveType.Added when pna.GetActiveItemInSlot(NamedSlot.feet)?.gameObject == gameObject:
				UIActionManager.ToggleLocal(this, true);
				UIActionManager.SetSpriteSO(this, spriteHandler.GetCurrentSpriteSO());
				break;
			case ClientInventoryMoveType.Removed when pna.GetActiveItemInSlot(NamedSlot.feet)?.gameObject != gameObject:
				UIActionManager.ToggleLocal(this, false);
				break;
		}
	}

	public void CallActionClient() { }

	public void CallActionServer(ConnectedPlayer SentByPlayer)
	{
		if (!SentByPlayer.Script.IsDeadOrGhost)
		{
			ServerChangeState(SentByPlayer);
		}
	}

	private bool ValidPlayer()
	{
		if (player == null || player.Script == null
		                   || player.Script.playerHealth == null) return false;
		return true;
	}
}
