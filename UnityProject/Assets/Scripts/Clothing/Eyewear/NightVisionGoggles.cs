using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using CameraEffects;

public class NightVisionGoggles : NetworkBehaviour, IServerInventoryMove
{
	[SerializeField, Tooltip("How far the player will be able to see in the dark while he has the goggles on.")]
	private Vector3 nightVisionVisibility;

	[SerializeField, Tooltip("The default minimal visibility size.")]
	private Vector3 defaultVisionVisibility;

	
	private void OnStartClient()
	{
		//(ThatDan123) : this wont work as equipment wont be sync'd in time for the check,
		if(PlayerManager.LocalPlayerScript != null)
		{
			var item = PlayerManager.LocalPlayerScript.Equipment.GetClothingItem(NamedSlot.eyes).GameObjectReference;
			if(item == gameObject) {enableEffect(true);}
		}
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
	
	[TargetRpc]
	private void TargetOnTakingOff(NetworkConnection target)
	{
		enableEffect(false);
	}

	[TargetRpc]
	private void TargetOnWearing(NetworkConnection target)
	{
		enableEffect(true);
	}

	private void enableEffect(bool check)
	{
		var camera = Camera.main;
		if(check == true)
		{
			camera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState();
			camera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(nightVisionVisibility);
		}
		else
		{
			camera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState();
			camera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(defaultVisionVisibility);
		}
	}
}

