using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraEffects;

public class NightVisionGoggles : MonoBehaviour, IServerInventoryMove
{
	[SerializeField, Tooltip("How far the player will be able to see in the dark while he has the goggles on.")]
	private Vector3 nightVisionVisibility;

	[SerializeField, Tooltip("The default minimal visibility size.")]
	private Vector3 defaultVisionVisibility;


	public void OnInventoryMoveServer(InventoryMove info)
		{
			RegisterPlayer registerPlayer;

			if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
			{
				registerPlayer = info.ToRootPlayer;

				if (registerPlayer != null && info.ToSlot.NamedSlot == NamedSlot.eyes)
				{
					OnWearing();
				}
			}

			if (info.FromSlot != null && info.FromSlot?.NamedSlot != null && info.ToSlot != null)
			{
				registerPlayer = info.FromRootPlayer;

				if (registerPlayer != null && info.FromSlot.NamedSlot == NamedSlot.eyes)
				{
					OnTakingOff();
				}
			}
		}
	
	private void OnTakingOff()
		{
			var camera = Camera.main;
			camera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState();
			camera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(defaultVisionVisibility);
		}

	private void OnWearing()
		{
			var camera = Camera.main;
			camera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState();
			camera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(nightVisionVisibility);
		}
}

