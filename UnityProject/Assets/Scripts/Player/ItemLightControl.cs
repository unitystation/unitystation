using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[Serializable]
public class PlayerLightData
{
	public float Intensity = 0.0f;
	public Color Colour;
	//todo Make it so badmins can Mess around with the sprite so It can be set to anything they desire
	//public Sprite Sprite;
	public EnumSpriteLightData EnumSprite;
	public float Size = 12;

}

public enum EnumSpriteLightData
{
	Default,
	Square,
	Clown,
}

[RequireComponent(typeof(Pickupable))]
public class ItemLightControl : NetworkBehaviour, IServerInventoryMove
{
	[Tooltip("Controls the light the player emits if they have this object equipped.")]
	public LightEmissionPlayer LightEmission;

	[Tooltip("Controls the light the object emits while out of a player's or other object's inventory.")]
	public GameObject objectLightEmission;

	public HashSet<NamedSlot> CompatibleSlots = new HashSet<NamedSlot>() {
		NamedSlot.leftHand,
		NamedSlot.rightHand,
		NamedSlot.suitStorage,
		NamedSlot.belt,
		NamedSlot.back,
		NamedSlot.storage01,
		NamedSlot.storage02,
		NamedSlot.suitStorage,
		NamedSlot.head
	};

	public float Intensity;

	[SerializeField]
	private Color Colour = default;

	//public Sprite Sprite;
	public EnumSpriteLightData EnumSprite;
	public float Size;

	[SyncVar(hook = nameof(SyncState))]
	public bool IsOn = true;

	private float CachedIntensity = 0.5f;

	public PlayerLightData PlayerLightData;

	private Light2D.LightSprite objectLightSprite;

	private void Awake()
	{
		if (objectLightEmission == null)
		{
			Debug.LogError($"{this} field objectLightEmission is null, please check {gameObject} prefab.");
			return;
		}

		objectLightSprite = objectLightEmission.GetComponent<Light2D.LightSprite>();
	}

	private void Start()
	{
		PlayerLightData = new PlayerLightData()
		{
			Intensity = Intensity,
			Colour = Colour,
			EnumSprite = EnumSprite,
			Size = Size,
		};
	}


	public void OnInventoryMoveServer(InventoryMove info)
	{
		//was it transferred from a player's visible inventory?
		if (info.FromPlayer != null && LightEmission != null)
		{
			LightEmission.RemoveLight(PlayerLightData);
			LightEmission = null;
		}

		if (info.ToPlayer != null)
		{
			if (CompatibleSlots.Contains(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)))
			{
				LightEmission = info.ToPlayer.GetComponent<LightEmissionPlayer>();
				if (!IsOn) return;
				LightEmission.AddLight(PlayerLightData);
			}
		}
	}

	/// <summary>
	/// Allows you to toggle the light
	/// </summary>
	public void Toggle(bool on)
	{
		if (IsOn == on) return;
		if (LightEmission == null)
		{
			Debug.LogError($"{this} field LightEmission is null, please check scripts.");
			return;
		}
		
		IsOn = on; // Will trigger SyncState.
		UpdateLights();
	}
	/// <summary>
	/// Changes the intensity of the light, must be higher than -1
	/// </summary>
	/// <param name="intensity"></param>
	public void SetIntensity(float intensity = -1)
	{
		if (PlayerLightData == null)
		{
			Debug.LogError("PlayerLightData returned Null please check scripts");
			return;
		}
		if (IsOn && intensity > -1)
		{
			//caches the intensity just incase and sets intensity
			CachedIntensity = intensity;
			PlayerLightData.Intensity = intensity;
		}
		else
		{
			//Sets the cached intensity so it the light will be set to that intensity when it is toggled on
			if(intensity <= -1) return;
			CachedIntensity = intensity;
		}
	}

	/// <summary>
	/// Set the color for this GameObject's Light2D object's LightSprite component world color.
	/// </summary>
	/// <param name="color"></param>
	public void SetColor(Color color)
	{
		Colour = color;
		PlayerLightData.Colour = color;
		objectLightSprite.Color = color;
	}

	private void SyncState(bool oldState, bool newState)
	{
		objectLightEmission.SetActive(newState);
	}

	private void UpdateLights()
	{
		if (IsOn)
		{
			LightEmission.AddLight(PlayerLightData);
			LightToggleIntensity();
			objectLightEmission.SetActive(true);
		}
		else
		{
			LightEmission.RemoveLight(PlayerLightData);
			objectLightEmission.SetActive(false);
		}
	}

	/// <summary>
	/// Called when the light is toggled on so that it can set the intensity
	/// </summary>
	private void LightToggleIntensity()
	{
		PlayerLightData.Intensity = CachedIntensity;
	}
}
