using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shared.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
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

	public Toggle Effects;
	public Toggle Walls;
	public Toggle Windows;
	public Toggle Grills;
	public Toggle Objects;
	public Toggle MapObjects;
	public Toggle Floors;
	public Toggle Tables;
	public Toggle Underfloor;
	public Toggle Electrical;
	public Toggle Pipes;
	public Toggle Disposals;
	public Toggle Base;


	private bool? Override = null;

	public void ToggleLayers()
	{
		SetLayerVisibility(Override ?? Effects.isOn, LayerType.Effects);
		SetLayerVisibility(Override ?? Walls.isOn, LayerType.Walls);
		SetLayerVisibility(Override ?? Windows.isOn, LayerType.Windows);
		SetLayerVisibility(Override ?? Grills.isOn, LayerType.Grills);
		SetObjectVisibility(Override ?? Objects.isOn);
		SetLayerVisibility(Override ?? Tables.isOn, LayerType.Tables);
		SetLayerVisibility(Override ?? Floors.isOn, LayerType.Floors);
		SetLayerVisibility(Override ?? Underfloor.isOn, LayerType.Underfloor);
		SetLayerVisibility(Override ?? Electrical.isOn, LayerType.Electrical);
		SetLayerVisibility(Override ?? Pipes.isOn, LayerType.Pipe);
		SetLayerVisibility(Override ?? Disposals.isOn, LayerType.Disposals);
		SetLayerVisibility(Override ?? Base.isOn, LayerType.Base);
		Override = null;
	}


	public bool GetObjectsMappingVisible()
	{
		if (this.gameObject.activeInHierarchy == false)
		{
			return true;
		}

		if (Objects.isOn == false)
		{
			return false;
		}

		if (MapObjects.isOn == false)
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	public HashSet<LayerType> ReturnVisibleLayers()
	{
		if (this.gameObject.activeInHierarchy == false)
		{
			return null;
		}

		HashSet<LayerType> Layers = new HashSet<LayerType>();
		if (Effects.isOn)
		{
			Layers.Add(LayerType.Effects);
		}

		if (Walls.isOn)
		{
			Layers.Add(LayerType.Walls);
		}

		if (Windows.isOn)
		{
			Layers.Add(LayerType.Windows);
		}

		if (Grills.isOn)
		{
			Layers.Add(LayerType.Grills);
		}

		if (Objects.isOn || MapObjects.isOn)
		{
			Layers.Add(LayerType.Objects);
		}

		if (Tables.isOn)
		{
			Layers.Add(LayerType.Tables);
		}

		if (Floors.isOn)
		{
			Layers.Add(LayerType.Floors);
		}

		if (Underfloor.isOn)
		{
			Layers.Add(LayerType.Underfloor);
		}

		if (Electrical.isOn)
		{
			Layers.Add(LayerType.Electrical);
		}


		if (Pipes.isOn)
		{
			Layers.Add(LayerType.Pipe);
		}

		if (Disposals.isOn)
		{
			Layers.Add(LayerType.Disposals);
		}

		if (Base.isOn)
		{
			Layers.Add(LayerType.Base);
		}

		return Layers;
	}


	public void SetObjectVisibility(bool Ison)
	{
		foreach (var Matrix in MatrixManager.Instance.ActiveMatrices)
		{
			Matrix.Value.Matrix.MetaTileMap.Layers[LayerType.Objects].gameObject.SetActive(Ison);
		}
	}

	public void SetLayerVisibility(bool Ison, LayerType LayerType)
	{
		foreach (var Matrix in MatrixManager.Instance.ActiveMatrices)
		{
			Matrix.Value.Matrix.MetaTileMap.Layers[LayerType].Tilemap.GetComponent<TilemapRenderer>().enabled = Ison;
		}
	}


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
		Override = null;
		ToggleLayers();
	}


	void ToggleLayerForCulling(bool state)
	{
		if (Camera.main == null) return;
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
