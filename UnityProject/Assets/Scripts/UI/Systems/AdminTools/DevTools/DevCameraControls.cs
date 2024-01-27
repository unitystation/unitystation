using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shared.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EscapeKeyTarget))]
public class DevCameraControls  : SingletonManager<DevCameraControls>
{
	public Button LightingButton;

	public TMP_Text LightingText;


	public Button MappingItemButton;

	public TMP_Text MappingItemText;

	public Color SelectedColour;
	public Color UnSelectedColour;

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	public bool Updating = false;

	private int? layerToToggle;

	private LightingSystem LightingSystem;

	private bool? LightingSystemState = null;

	public bool MappingItemState = false;


	private void OnEnable()
	{
		if (Updating == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			Updating = true;
		}

		if (escapeKeyTarget != null)
		{
			escapeKeyTarget.enabled = true;
		}
	}

	public override void Start()
	{
		base.Start();
		this.gameObject.SetActive(false);
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		// Set the layer to toggle (change this to the desired layer)
		layerToToggle = LayerMask.NameToLayer("Editor View Only");

	}

	private void OnDisable()
	{
		OnEscape();
		if (Updating)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			Updating = false;
		}
	}


	private void UpdateMe()
	{
		if (LightingSystem == null)
		{
			LightingSystem = Camera.main.GetComponent<LightingSystem>();
		}

		if (LightingSystem.enabled != LightingSystemState)
		{
			LightingSystemState = LightingSystem.enabled;
			if (LightingSystemState.Value)
			{
				LightingText.text = @"Turn Lighting
Off";
				var ColorBlock = LightingButton.colors;
				ColorBlock.normalColor = SelectedColour;
				LightingButton.colors = ColorBlock;
			}
			else
			{
				LightingText.text = @"Turn Lighting
On";
				var ColorBlock = LightingButton.colors;
				ColorBlock.normalColor = UnSelectedColour;
				LightingButton.colors = ColorBlock;
			}
		}
	}




	public void CloseButton()
	{
		this.gameObject.SetActive(false);
	}

	public void OnEscape()
	{
		//stop drawing
		if (Updating)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			Updating = false;
		}

		if (escapeKeyTarget != null)
		{
			escapeKeyTarget.enabled = false;
		}

		if (Camera.main.OrNull()?.GetComponent<LightingSystem>() != null)
		{
			Camera.main.GetComponent<LightingSystem>().enabled = true;
		}
		ToggleLayerForCulling(false);
	}


	void ToggleLayerForCulling(bool state)
	{
		int currentCullingMask = Camera.main.cullingMask;
		if (layerToToggle == null) return;
		int layerMaskToToggle = 1 << layerToToggle.Value;

		// Check if the layer is currently included in the culling mask
		if ((currentCullingMask & layerMaskToToggle) != 0 && state == false)
		{
			MappingItemState = false;
			// Layer is currently included, so exclude it
			Camera.main.cullingMask &= ~layerMaskToToggle;

			MappingItemText.text = @"Turn Mapping
View On";
			var ColorBlock = MappingItemButton.colors;
			ColorBlock.normalColor = UnSelectedColour;
			MappingItemButton.colors = ColorBlock;
		}
		else if (state)
		{
			// Layer is currently excluded, so include it
			MappingItemState = true;
			Camera.main.cullingMask |= layerMaskToToggle;

			MappingItemText.text = @"Turn Mapping
View Off";
			var ColorBlock = MappingItemButton.colors;
			ColorBlock.normalColor = SelectedColour;
			MappingItemButton.colors = ColorBlock;
		}
	}

	public void OnSelectedLightingSystem()
	{
		LightingSystem.enabled = !LightingSystem.enabled;
	}


	[NaughtyAttributes.Button]
	public void OnSelectedMappingItems()
	{
		ToggleLayerForCulling(!MappingItemState);
	}
}
