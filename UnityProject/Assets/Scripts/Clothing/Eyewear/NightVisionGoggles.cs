using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using CameraEffects;
using UI.Action;

public class NightVisionGoggles : NetworkBehaviour, IServerInventoryMove, ICheckedInteractable<HandActivate>
{
	[SerializeField, Tooltip("How far the player will be able to see in the dark while he has the goggles on.")]
	private Vector3 nightVisionVisibility;

	[SerializeField, Tooltip("The default minimal visibility size.")]
	private Vector3 defaultVisionVisibility;

	[SerializeField, Tooltip("How fast will the player gain visibility?")]
	private float visibilityAnimationSpeed = 1.50f;

	private bool isOn = true;
	private ItemActionButton actionButton;
	private Camera mainCamera;

	private RegisterPlayer currentPlayer;

	#region LifeCycle

	private void Awake()
	{
		actionButton = GetComponent<ItemActionButton>();
		mainCamera = Camera.main;
	}

	private void OnEnable()
	{
		actionButton.ServerActionClicked += ToggleGoggles;
	}

	private void OnDisable()
	{
		actionButton.ServerActionClicked -= ToggleGoggles;
	}

	public override void OnStartClient()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestNightVisionState(gameObject, PlayerManager.LocalPlayer);

		base.OnStartClient();
	}

	#endregion

	#region InventoryMove

	//IServerInventoryMove should be replaced with IClientInventoryMove but that needs more functionality first
	public void OnInventoryMoveServer(InventoryMove info)
	{
		currentPlayer = info.ToRootPlayer;

		//Equipping goggles
		if (info.ToSlot?.NamedSlot != null)
		{
			if (currentPlayer != null && info.ToSlot.NamedSlot == NamedSlot.eyes)
			{
				//Only turn on goggle for client if they are on
				if(isOn == false) return;

				ServerToggleClient(true);
			}
		}

		//Removing goggles
		if (info.FromSlot?.NamedSlot != null)
		{
			if (currentPlayer != null && info.FromSlot.NamedSlot == NamedSlot.eyes)
			{
				//Always try to turn client off when removing
				ServerToggleClient(false);
				currentPlayer = null;
			}
		}
	}

	#endregion

	#region HandInteract

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		isOn = !isOn;
		Chat.AddExamineMsgToClient($"You turned {(isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
	}

	#endregion

	[Server]
	private void ToggleGoggles()
	{
		SetGoggleState(!isOn);
	}

	[Server]
	private void SetGoggleState(bool newState)
	{
		if(currentPlayer == null || currentPlayer.connectionToClient == null) return;

		isOn = newState;
		var item = currentPlayer.PlayerScript.Equipment.GetClothingItem(NamedSlot.eyes).OrNull()?.GameObjectReference;
		if (item == gameObject)
		{
			RpcToggleGoggles(currentPlayer.connectionToClient, isOn);
			Chat.AddExamineMsgFromServer(currentPlayer.gameObject, $"You turned {(isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
		}
	}

	[Server]
	private void ServerToggleClient(bool newState)
	{
		if(currentPlayer == null || currentPlayer.connectionToClient == null) return;

		RpcToggleGoggles(currentPlayer.connectionToClient, newState);
	}

	[TargetRpc]
	private void RpcToggleGoggles(NetworkConnection target, bool state)
	{
		EnableEffect(state);
	}

	[Client]
	private void EnableEffect(bool newState)
	{
		if(mainCamera == null) return;

		mainCamera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(nightVisionVisibility, newState ? visibilityAnimationSpeed : 0.1f);
		mainCamera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState(newState);
	}

	/// <summary>
	/// Resyncs clients when joining
	/// </summary>
	/// <param name="player"></param>
	[Server]
	public void ReSyncClient(GameObject player)
	{
		if(currentPlayer == null || currentPlayer.connectionToClient == null) return;

		//Only toggle equipped player
		if(player != currentPlayer.gameObject) return;

		RpcToggleGoggles(currentPlayer.connectionToClient, isOn);
	}
}

