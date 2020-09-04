using System;
using UnityEngine;
using UnityEngine.UI;
using Objects.GasContainer;

/// <summary>
/// Main logic for the release lever.
/// </summary>
public class ReleaseLever : MonoBehaviour
{
	//the shadow on the upper part of the lever
	public Shadow upperShadow;
	public float ShadowDistance = 10;
	private bool muteSounds;

	private void Awake()
	{
		muteSounds = GetComponentInParent<GUI_Canister>().IsServer;
	}

	public void OnToggled(bool isOpen)
	{
		//play toggle sound
		if (!muteSounds)
		{
			SoundManager.Play("Valve", 0.1f, pan: 0.3f);
		}

		//fix the shadow and rotate
		if (isOpen)
		{
			transform.rotation = Quaternion.Euler(0,0,90);
			upperShadow.effectDistance = new Vector2(-ShadowDistance, -ShadowDistance);
		}
		else
		{
			transform.rotation = Quaternion.identity;
			upperShadow.effectDistance = new Vector2(ShadowDistance, -ShadowDistance);
		}
	}

}
