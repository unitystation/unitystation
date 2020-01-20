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

	private Image image;
	private LightingSystem lighting;

	private void Awake()
	{
		image = GetComponent<Image>();

		// Get the lighting system
		lighting = Camera.main.GetComponent<LightingSystem>();
		if (!lighting)
		{
			Logger.LogWarning("Ghost UI can't find lighting system on Camera.main", Category.Lighting);
			return;
		}
	}

	private void OnEnable()
	{
		if (!lighting)
			return;

		// subscribe to lighting system change (can disabled from other systems)
		lighting.OnLightingSystemEnabled += OnLightingSystemEnabled;
		// update sprite
		UpdateSprite();
	}

	private void OnDisable()
	{
		if (!lighting)
			return;

		// unsubscribe from lighting system
		lighting.OnLightingSystemEnabled -= OnLightingSystemEnabled;
	}

	/// <summary>
	/// Toggle on/off lighting system
	/// </summary>
	public void OnLightTogglePressed()
	{
		if (!lighting)
			return;

		// Change lighting system state to opposite
		// Image sprite will change by event
		var isLighitingEnabled = lighting.enabled;
		lighting.enabled = !isLighitingEnabled;
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
		if (image)
		{
			image.sprite = lighting.enabled ? lightOnSprite : lightOffSprite;
		}
	}

}
