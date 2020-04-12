using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.UI;

public class ItemMagBoots : NetworkBehaviour,
	IServerActionGUI, IClientInventoryMove,IServerInventoryMove
{
	[SyncVar(hook = nameof(SyncState))]
	private bool isOn = false;

	public GameObject spriteObject;
	private SpriteHandler spriteHandler;

	[Tooltip("For UI button, 0 = off , 1 = on")]
	public Sprite[] sprites;

	public  SpriteSheetAndData[] spriteSheets;

	private ConnectedPlayer player;
	private ItemAttributesV2 itemAttributesV2;
	private Pickupable pick;

	[Tooltip("The speed debuff to apply to run speed.")]
	[SerializeField]
	private float runSpeedDebuff = 1.5f;


	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	private PlayerMove playerMove;

	private void Awake()
	{
		pick = GetComponent<Pickupable>();
		spriteHandler = spriteObject.GetComponent<SpriteHandler>();
		itemAttributesV2 = GetComponent<ItemAttributesV2>();
		pick.RefreshUISlotImage();
	}

	private void ToggleBoots()
	{
		if(player == null )
		{
			return;
		}
		if (isOn)
		{
			itemAttributesV2.AddTrait(CommonTraits.Instance.NoSlip);
			ApplySpeedDebuff();

		}
		else
		{
			itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
			RemoveSpeedDebuff();
		}
		//if the ghost NRE will be thrown
		player.Script.pushPull.ServerSetPushable(!isOn);
	}

	private void SyncState(bool oldVar, bool newVar)
	{
		isOn = newVar;
		spriteHandler.ChangeSprite(isOn ? 1 : 0);
		pick.RefreshUISlotImage();
		ToggleBoots();
	}

	public void ServerChangeState(ConnectedPlayer newPlayer)
	{
		this.player = newPlayer;
		isOn = !isOn;
	}

	private void OnPlayerDeath()
	{
		if (isServer)
		{
			if (isOn)
			{
				ServerChangeState(this.player);
			}
			UIActionManager.Toggle(this, false);
			// player.Script.playerHealth.OnDeathNotifyEvent -= OnPlayerDeath;
			player = null;
		}
		else
		{
			UIActionManager.Toggle(this, false);
		}

	}

	private void ApplySpeedDebuff()
	{
		playerMove.ServerChangeSpeed(run: playerMove.runSpeed - runSpeedDebuff);
	}

	private void RemoveSpeedDebuff()
	{
		playerMove.ServerChangeSpeed(run: playerMove.runSpeed + runSpeedDebuff);
	}

	#region UI related

	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (IsPuttingOn(info) & isOn)
		{
			ApplySpeedDebuff();
		}
		else if (IsTakingOff(info) & isOn)
		{
			ServerChangeState(player);
			RemoveSpeedDebuff();
		}
	}

	private bool IsPuttingOn (InventoryMove info)
	{
		if (info.ToSlot == null | info.ToSlot?.NamedSlot == null)
		{
			return false;
		}
		playerMove = info.ToRootPlayer?.PlayerScript.playerMove;

		return playerMove != null && info.ToSlot.NamedSlot == NamedSlot.feet;
	}

	private bool IsTakingOff (InventoryMove info)
	{
		if (info.FromSlot == null | info.FromSlot?.NamedSlot == null)
		{
			return false;
		}
		playerMove = info.FromRootPlayer?.PlayerScript.playerMove;

		return playerMove != null && info.FromSlot.NamedSlot == NamedSlot.feet;
	}

	// Client only method
	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		var hand = PlayerManager.LocalPlayerScript.playerNetworkActions;
		//when item is moved and player has it on his feet
		if (hand.GetActiveItemInSlot(NamedSlot.feet) != null && hand.GetActiveItemInSlot(NamedSlot.feet) == gameObject)
		{
			UIActionManager.Toggle(this, true);
			UIActionManager.SetSprite(this, (sprites[0]));
		}
		else
		{
			UIActionManager.Toggle(this, false);
		}
	}

	public void CallActionClient()
	{
		UIActionManager.SetSprite(this, (!isOn ? sprites[1] : sprites[0]));
	}

	public void CallActionServer(ConnectedPlayer SentByPlayer)
	{
		ServerChangeState(SentByPlayer);
	}

	#endregion
}