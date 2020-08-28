using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Atmospherics;

public class ItemMagBoots : NetworkBehaviour,
	IServerActionGUI, IClientInventoryMove, IServerInventoryMove
{
	[Tooltip("For UI button, 0 = off, 1 = on.")]
	[SerializeField]
	private Sprite[] sprites = new Sprite[2];

	[Tooltip("The speed debuff to apply to run speed.")]
	[SerializeField]
	private float runSpeedDebuff = 1.5f;

	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	private SpriteHandler spriteHandler;
	private ConnectedPlayer player;
	private ItemAttributesV2 itemAttributesV2;
	private Pickupable pick;
	private PlayerMove playerMove;

	private bool isOn = false;

	private enum SpriteState
	{
		Off = 0,
		On = 1
	}

	private void Awake()
	{
		pick = GetComponent<Pickupable>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		itemAttributesV2 = GetComponent<ItemAttributesV2>();
		pick.RefreshUISlotImage();
	}

	private void ToggleState()
	{
		isOn = !isOn;
		spriteHandler.ChangeSprite(isOn ? (int) SpriteState.On : (int) SpriteState.Off);
		//pick.RefreshUISlotImage();
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
				UIActionManager.SetSprite(this, sprites[(int) SpriteState.Off]);
				break;
			case ClientInventoryMoveType.Removed when pna.GetActiveItemInSlot(NamedSlot.feet)?.gameObject != gameObject:
				UIActionManager.ToggleLocal(this, false);
				break;
		}
	}

	public void CallActionClient()
	{
		if (!PlayerManager.LocalPlayerScript.IsDeadOrGhost)
		{
			int index = isOn ? (int) SpriteState.On : (int) SpriteState.Off;
			UIActionManager.SetSprite(this, index);
		}
	}

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
