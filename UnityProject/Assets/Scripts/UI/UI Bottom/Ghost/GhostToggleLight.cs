using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI element to control ghost FOV and lighting vision
/// </summary>
[RequireComponent(typeof(Image))]
public class GhostToggleLight : MonoBehaviour
{
	public Sprite lightOnSprite;
	public Sprite lightOffSprite;

	[SerializeField]
	private Image image = null;

	private LightingSystem lighting;
	/// <summary>
	/// Safely resolve lighting system dependency
	/// </summary>
	private LightingSystem LightingSys
	{
		get
		{
			// Already have lighting link?
			if (lighting)
				return lighting;

			// Check camera main
			if (Camera.main == null)
			{
				//means that the client may of logged out
				//and the scene is in between loading
				return null;
			}

			// Get the lighting system
			lighting = Camera.main.GetComponent<LightingSystem>();
			return lighting;
		}
	}

	private void OnEnable()
	{
		if (!LightingSys)
			return;

		// subscribe to lighting system change (can be disabled from other systems, like admin spawn)
		LightingSys.OnLightingSystemEnabled += OnLightingSystemEnabled;
		// update sprite
		UpdateSprite();
	}

	private void OnDisable()
	{
		if (!LightingSys)
			return;

		// unsubscribe from lighting system
		LightingSys.OnLightingSystemEnabled -= OnLightingSystemEnabled;
	}

	/// <summary>
	/// Toggle on/off lighting system
	/// </summary>
	public void OnLightTogglePressed()
	{
		if (!LightingSys)
			return;

		// Change lighting system state to opposite
		// Image sprite will change by event
		var isLighitingEnabled = LightingSys.enabled;
		LightingSys.enabled = !isLighitingEnabled;
	}

	private void OnLightingSystemEnabled(bool isEnabled)
	{
		UpdateSprite();
	}

	/// <summary>
	/// Validate that sprite shows correct state of lighting system
	/// </summary>
	private void UpdateSprite()
	{
		image.sprite = LightingSys.enabled ? lightOnSprite : lightOffSprite;
	}

}
