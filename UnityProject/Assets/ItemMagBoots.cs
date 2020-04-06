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
	[Tooltip("Default player run speed.")]
	[SerializeField]
	private float initialSpeed = 6;

	[Tooltip("Change player run speed to this.")]
	[SerializeField]
	private float newSpeed = 4.5f;

	private float oldSpeed;

	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

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
			ServerChangeSpeed(newSpeed);

		}
		else
		{
			itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
			ServerChangeSpeed(initialSpeed);
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

	#region UI related

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//If item was taken off the player and it's on, change state back
		if (isOn && info.FromSlot.NamedSlot == NamedSlot.feet)
		{
			ServerChangeState(this.player);
		}
	}

	// Client only method
	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		if (CustomNetworkManager.Instance._isServer && GameData.IsHeadlessServer)
		{
			return;
		}

		var hand = PlayerManager.LocalPlayerScript.playerNetworkActions;
		//when item is moved and player has it on his feet
		if (hand.GetActiveItemInSlot(NamedSlot.feet) != null && hand.GetActiveItemInSlot(NamedSlot.feet) == gameObject)
		{
			UIActionManager.Toggle(this, true);
			UIActionManager.SetSprite(this, (sprites[0]));
		}
		else
		{
			if (isOn)
			{
				ClientChangeSpeed(initialSpeed);//old
			}
			UIActionManager.Toggle(this, false);
		}
	}

	public void CallActionClient()
	{
		UIActionManager.SetSprite(this, (!isOn ? sprites[1] : sprites[0]));
		var hand = PlayerManager.LocalPlayerScript.playerNetworkActions;
		//if player has it on his feet
		if (hand.GetActiveItemInSlot(NamedSlot.feet) != null && hand.GetActiveItemInSlot(NamedSlot.feet) == gameObject)
		{
			if (!isOn)
			{
				ClientChangeSpeed(newSpeed);//newspeed
			}
			else
			{
				ClientChangeSpeed(initialSpeed);//old
			}
		}
	}

	private void ClientChangeSpeed(float speed)
	{
		PlayerMove playerMove = PlayerManager.LocalPlayerScript.playerMove;
		PlayerSync playerSync = playerMove.GetComponent<PlayerSync>();


		playerMove.InitialRunSpeed = speed;
		playerMove.RunSpeed = speed;

		//Do not change current speed if player is walking
		//but change speed when he toggles run
		if (playerSync.SpeedServer < speed)
		{ return;
		}

		playerSync.SpeedClient = speed;

	}
	public void CallActionServer(ConnectedPlayer SentByPlayer)
	{
		ServerChangeState(SentByPlayer);
	}

	private void ServerChangeSpeed(float speed)
	{
		player.Script.playerMove.InitialRunSpeed = speed;
		player.Script.playerMove.RunSpeed = speed;
		//Do not change current speed if player is walking
		//but change speed when he toggles run
		if (player.Script.PlayerSync.SpeedServer < speed)
		{
			return;
		}
		player.Script.PlayerSync.SpeedServer  = speed;
	}

	#endregion
}
