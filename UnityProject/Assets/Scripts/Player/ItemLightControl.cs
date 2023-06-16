using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using Mirror;

[Serializable]
public class PlayerLightData
{
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
public class ItemLightControl : BodyPartFunctionality, IServerInventoryMove
{
	[Tooltip("Controls the light the player emits if they have this object equipped.")]
	public LightEmissionPlayer LightEmission;

	[Tooltip("Controls the light the object emits while out of a player's or other object's inventory.")]
	public GameObject objectLightEmission;

	public CommonComponents CommonComponents;

	public HashSet<NamedSlot> CompatibleSlots = new HashSet<NamedSlot>() {
		NamedSlot.leftHand,
		NamedSlot.rightHand,
		NamedSlot.suitStorage,
		NamedSlot.belt,
		NamedSlot.back,
		NamedSlot.storage01, NamedSlot.storage02, NamedSlot.storage03, NamedSlot.storage04,
		NamedSlot.storage05, NamedSlot.storage06, NamedSlot.storage07, NamedSlot.storage08,
		NamedSlot.storage09, NamedSlot.storage10,
		NamedSlot.suitStorage,
		NamedSlot.head,
		NamedSlot.id // PDA in ID slot
	};

	[SerializeField]
	private Color Colour = default;

	//public Sprite Sprite;
	public EnumSpriteLightData EnumSprite;
	public float Size;

	[SyncVar(hook = nameof(SyncState))]
	public bool IsOn = true;

	[NaughtyAttributes.ReadOnlyAttribute] public PlayerLightData PlayerLightData;

	private Light2D.LightSprite objectLightSprite;

	private void Awake()
	{
		PlayerLightData = new PlayerLightData()
		{
			Colour = Colour,
			EnumSprite = EnumSprite,
			Size = Size,
		};
		CommonComponents = this.GetComponent<CommonComponents>();
		CommonComponents.RegisterTile.OnAppearClient.AddListener(StateHiddenChange);
		CommonComponents.RegisterTile.OnDisappearClient.AddListener(StateHiddenChange);
		CommonComponents.UniversalObjectPhysics.OnVisibilityChange += StateHiddenChange;

		if (objectLightEmission == null)
		{
			Logger.LogError($"{this} field objectLightEmission is null, please check {gameObject} prefab.", Category.Lighting);
			return;
		}

		objectLightSprite = objectLightEmission.GetComponent<Light2D.LightSprite>();
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


	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		LightEmission.RemoveLight(PlayerLightData);
		LightEmission = null;
	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		LightEmission = livingHealth.GetComponent<LightEmissionPlayer>();
		if (!IsOn) return;
		LightEmission.AddLight(PlayerLightData);
	} //Warning only add body parts do not remove body parts in this

	/// <summary>
	/// Allows you to toggle the light
	/// </summary>
	public void Toggle(bool on)
	{
		if (IsOn == on) return;
		if (LightEmission == null)
		{
			Logger.LogError($"{this} field LightEmission is null, please check scripts.", Category.Lighting);
			return;
		}

		IsOn = on; // Will trigger SyncState.
		UpdateLights();
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
		if (LightEmission != null && IsOn)
		{
			LightEmission.UpdateDominantLightSource();
		}
	}

	public void SetSize(int Size)
	{
		this.Size = Size;
		PlayerLightData.Size = Size;
		//objectLightSprite = Size; TODO
		if (LightEmission != null && IsOn)
		{
			LightEmission.UpdateDominantLightSource();
		}
	}

	private void SyncState(bool oldState, bool newState)
	{
		IsOn = newState;
		if (CommonComponents.UniversalObjectPhysics.IsVisible == false)
		{
			objectLightEmission.SetActive(newState);
		}
	}


	private void UpdateLights()
	{
		if (IsOn)
		{
			LightEmission.AddLight(PlayerLightData);
			objectLightEmission.SetActive(true);
		}
		else
		{
			LightEmission.RemoveLight(PlayerLightData);
			objectLightEmission.SetActive(false);
		}
	}


	private void StateHiddenChange()
	{
		if (CommonComponents.UniversalObjectPhysics.IsVisible == false)
		{
			objectLightEmission.SetActive(false);
		}
		else
		{
			objectLightEmission.SetActive(IsOn);
		}
	}
}
