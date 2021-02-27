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

	private Pickupable pickupable;
	private ItemActionButton actionButton;
	
	private void OnStartClient()
	{
		//(ThatDan123) : this wont work as equipment wont be sync'd in time for the check,
		if(PlayerManager.LocalPlayerScript != null)
		{
			var item = getWhatsOnThePlayerEyes();
			if(item == gameObject) {enableEffect(true);}
		}
	}
	private void Awake()
	{
		pickupable = GetComponent<Pickupable>();
		actionButton = GetComponent<ItemActionButton>();
	}

	private void OnEnable()
	{
		actionButton.ServerActionClicked += turnOnGoggles;
	}

	private void OnDisable()
	{
		actionButton.ServerActionClicked -= turnOnGoggles;
	}

	//IServerInventoryMove should be replaced with IClienInventoryMove but that needs more functionality first
	public void OnInventoryMoveServer(InventoryMove info)
	{
		RegisterPlayer registerPlayer = info.ToRootPlayer;

		if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
		{

			if (registerPlayer != null && info.ToSlot.NamedSlot == NamedSlot.eyes)
			{
				TargetOnWearing(registerPlayer.connectionToClient);
			}
		}

		if (info.FromSlot != null && info.FromSlot?.NamedSlot != null)
		{
			if (registerPlayer != null && info.FromSlot.NamedSlot == NamedSlot.eyes)
			{
				TargetOnTakingOff(registerPlayer.connectionToClient);
			}
		}
	}
	
	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		turnOnGoggles();
	}

	private void turnOnGoggles()
	{
		if(isOn == true)
		{
			isOn = false;
			var item = getWhatsOnThePlayerEyes();
			if(item == gameObject) {enableEffect(false);}
			Chat.AddExamineMsgToClient($"You turned off the {gameObject.ExpensiveName()}.");
		}
		else
		{
			isOn = true;
			var item = getWhatsOnThePlayerEyes();
			if(item == gameObject) {enableEffect(true);}
			Chat.AddExamineMsgToClient($"You turned on the {gameObject.ExpensiveName()}.");
		}
	}

	private GameObject getWhatsOnThePlayerEyes()
	{
		return PlayerManager.LocalPlayerScript.Equipment.GetClothingItem(NamedSlot.eyes).GameObjectReference;
	}

	private void enableEffect(bool check)
	{
		var camera = Camera.main;
		if(check == true)
		{
			camera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState();
			camera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(nightVisionVisibility, visibilityAnimationSpeed);
		}
		else
		{
			camera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState();
			camera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(defaultVisionVisibility, 0.1f);
		}
	}

	[TargetRpc]
	private void TargetOnTakingOff(NetworkConnection target)
	{
		if(isOn == true){enableEffect(false);}
	}

	[TargetRpc]
	private void TargetOnWearing(NetworkConnection target)
	{
		if(isOn == true){enableEffect(true);}
	}
}

