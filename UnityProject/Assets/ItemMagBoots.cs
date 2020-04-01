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
		IActionGUI, IClientInventoryMove
{
	[SyncVar(hook = nameof(SyncState))]
	private bool isOn = false;

	public GameObject spriteObject;
	private SpriteHandler spriteHandler;

	[Tooltip("For UI button, 0 = off , 1 = on")]
	public Sprite[] sprites;

	private GameObject player;
	private ItemAttributesV2 itemAttributesV2;
	private Pickupable pick;

	[Tooltip("Change player run speed to this.")]
	[SerializeField]
	private float newSpeed = 4.5f;

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
		if (isOn)
		{
			itemAttributesV2.AddTrait(CommonTraits.Instance.NoSlip);
			//ChangeSpeed(newSpeed);
		}
		else
		{
			itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
			//ChangeSpeed(6);
		}
		if(player == null )
		{
			Debug.Log("Player == null");
			return;
		}

		player.GetComponent<ObjectBehaviour>()?.ServerSetPushable(!isOn);
		//Debug.Log("MagBoots are " + isOn.ToString());
	}

	private void ChangeSpeed(float speed)
	{
		PlayerSync playerSync = player.GetComponent<PlayerScript>().GetComponent<PlayerSync>();
		/*
		Debug.Log("Initial Run speed before change = " + playerSync.playerMove.InitialRunSpeed.ToString());
		Debug.Log("RunSpeed before change = " + playerSync.playerMove.RunSpeed.ToString());
		Debug.Log("WalkSpeed before change = " + playerSync.playerMove.WalkSpeed.ToString());
		Debug.Log("ServerSpeed before after = " + playerSync.SpeedServer.ToString());*/

		playerSync.playerMove.InitialRunSpeed = speed;
		playerSync.playerMove.RunSpeed = speed;

		//Do not change current speed if player is walking
		//but change speed when he toggles run
		if (playerSync.SpeedServer == playerSync.playerMove.WalkSpeed)
		{
			return;
		}
		playerSync.SpeedServer  = speed;
		/*
		Debug.Log("Initial Run speed after change = " + playerSync.playerMove.InitialRunSpeed.ToString());
		Debug.Log("RunSpeed before after = " + playerSync.playerMove.RunSpeed.ToString());
		Debug.Log("WalkSpeed before after = " + playerSync.playerMove.WalkSpeed.ToString());
		Debug.Log("ServerSpeed after after = " + playerSync.SpeedServer.ToString());*/
	}

	private void SyncState(bool oldVar, bool newVar)
	{
		isOn = newVar;
		spriteHandler.ChangeSprite(isOn ? 1 : 0);
		pick.RefreshUISlotImage();
		ToggleBoots();
	}

	[Server]
	public void ServerChangeState(GameObject newPlayer)
	{
		this.player = newPlayer;
		isOn = !isOn;
	}

	#region UI related

	// Client only method
	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		//IClientInventoryMove method
		if (CustomNetworkManager.Instance._isServer && GameData.IsHeadlessServer)
		{
			return;
		}

		var hand = PlayerManager.LocalPlayerScript.playerNetworkActions;
		//when item is moved and player has it on his feet
		var showAlert = hand.GetActiveItemInSlot(NamedSlot.feet) == gameObject;

		//If item was taken off the player and it's on, change state back
		if (isOn && info.ClientInventoryMoveType == ClientInventoryMoveType.Removed && hand.GetActiveItemInSlot(NamedSlot.feet) == null)
		{
			ServerChangeState(this.player);
			//Debug.Log("Item WAS REMOVED WHILE BEING ON!.");
		}
		//shows UI button on screen
		UIActionManager.Toggle(this, showAlert);
	}

	public void CallActionClient()
	{
		UIActionManager.SetSprite(this, (!isOn ? sprites[1] : sprites[0]));
		//Debug.Log("Toggle state.");
		PlayerManager.PlayerScript.playerNetworkActions.CmdToggleMagBoots();
		/*In order to have UI button, add button to Alert_UI_HUD in unity
		define functions in AlertUI.cs, for button logic
		example (ToggleAlertMagBoots and OnClickMagBoots)
		define a function in PlayerNetworkActions.cs
		example (CmdToggleMagBoots)
		so you can call it from here and AlertUI */
	}

	#endregion
}
