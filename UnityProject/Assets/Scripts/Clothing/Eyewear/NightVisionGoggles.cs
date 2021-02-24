using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraEffects;

public class NightVisionGoggles : MonoBehaviour, IClientInventoryMove
{
	[SerializeField, Tooltip("How far the player will be able to see in the dark while he has the goggles on.")]
	private Vector3 nightVisionVisibility;

	[SerializeField, Tooltip("The default minimal visibility size.")]
	private Vector3 defaultVisionVisibility;



	public void OnInventoryMoveClient(ClientInventoryMove info)
		{
			var registerPlayer = PlayerManager.LocalPlayerScript;

			if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
			{

				if (registerPlayer != null && info.ToSlot.NamedSlot == NamedSlot.eyes)
				{
					OnWearing();
				}
			}

			if (info.FromSlot != null && info.FromSlot?.NamedSlot != null)
			{
				if (registerPlayer != null && info.FromSlot.NamedSlot == NamedSlot.eyes)
				{
					OnTakingOff();
				}
			}
		}

	private void OnTakingOff()
		{
			enableEffect(false);
		}

	private void OnWearing()
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

