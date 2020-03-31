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
		IActionGUI,IServerInventoryMove, IClientInventoryMove
{
	[SyncVar(hook = nameof(SyncState))]
	private bool isOn = false;

	private GameObject player;
	private ItemAttributesV2 itemAttributesV2;

	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	private void Awake()
	{
		itemAttributesV2 = GetComponent<ItemAttributesV2>();
	}


	private void ToggleBoots()
	{
		PlayerSync playerSync = player.GetComponent<PlayerSync>();
		if (isOn)
		{
			Debug.Log("Initial Run speed before change = " + playerSync.playerMove.InitialRunSpeed.ToString());
			Debug.Log("RunSpeed before change = " + playerSync.playerMove.RunSpeed.ToString());
			Debug.Log("WalkSpeed before change = " + playerSync.playerMove.WalkSpeed.ToString());
			Debug.Log("ServerSpeed before after = " + playerSync.SpeedServer.ToString());

			playerSync.playerMove.InitialRunSpeed = 5;
			playerSync.playerMove.RunSpeed = 5;

			playerSync.SpeedServer  = 5;

			Debug.Log("Initial Run speed after change = " + playerSync.playerMove.InitialRunSpeed.ToString());
			Debug.Log("RunSpeed before after = " + playerSync.playerMove.RunSpeed.ToString());
			Debug.Log("WalkSpeed before after = " + playerSync.playerMove.WalkSpeed.ToString());
			Debug.Log("ServerSpeed after after = " + playerSync.SpeedServer.ToString());

			itemAttributesV2.AddTrait(CommonTraits.Instance.NoSlip);
		}
		else
		{
			Debug.Log("Initial Run speed before change = " + playerSync.playerMove.InitialRunSpeed.ToString());
			Debug.Log("RunSpeed before change = " + playerSync.playerMove.RunSpeed.ToString());
			Debug.Log("WalkSpeed before change = " + playerSync.playerMove.WalkSpeed.ToString());
			Debug.Log("ServerSpeed before after = " + playerSync.SpeedServer.ToString());

			playerSync.playerMove.InitialRunSpeed = 6;
			playerSync.playerMove.RunSpeed = 6;
			playerSync.SpeedServer  = 6;

			Debug.Log("Initial Run speed after change = " + playerSync.playerMove.InitialRunSpeed.ToString());
			Debug.Log("RunSpeed before after = " + playerSync.playerMove.RunSpeed.ToString());
			Debug.Log("WalkSpeed before after = " + playerSync.playerMove.WalkSpeed.ToString());
			Debug.Log("ServerSpeed after after = " + playerSync.SpeedServer.ToString());

			itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
		}
		player.GetComponent<ObjectBehaviour>().ServerSetPushable(!isOn);
		Debug.Log("MagBoots are " + isOn.ToString());
	}

	private void SyncState(bool oldVar, bool newVar)
	{
		isOn = newVar;
		ToggleBoots();
	}
	[Server]
	public void ServerChangeState(GameObject player)
	{
		this.player = player;
		isOn = !isOn;
	}
	public void OnInventoryMoveServer(InventoryMove info)
	{
		//stop any observers (except for owner) from observing it if it's moved
		var fromRootPlayer = info.FromRootPlayer;

	}
	
	// Client only method
	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		if (CustomNetworkManager.Instance._isServer && GameData.IsHeadlessServer)
		{
			return;
		}
		var hand = PlayerManager.LocalPlayerScript.playerNetworkActions;
		var showAlert = hand.GetActiveItemInSlot(NamedSlot.feet) == gameObject;

		UIActionManager.Toggle(this, showAlert);
	}
	public void CallActionClient()
	{
		Debug.Log("Toggle state.");
		PlayerManager.PlayerScript.playerNetworkActions.CmdToggleMagBoots();
	}
}
