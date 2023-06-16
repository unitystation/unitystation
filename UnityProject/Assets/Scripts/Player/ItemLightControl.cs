using System;
using System.Collections.Generic;
using Core.Lighting;
using HealthV2;
using Light2D;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using UnityEngine.Serialization;

[RequireComponent(typeof(Pickupable))]
public class ItemLightControl : BodyPartFunctionality, IServerInventoryMove
{
	[Tooltip("Controls the light the player emits if they have this object equipped.")]
	public LightsHolder LightEmission;

	[Tooltip("Controls the light the object emits while out of a player's or other object's inventory.")]
	public GameObject objectLightEmission;

	[SyncVar(hook = nameof(SyncState))]
	public bool IsOn = true;

	[FormerlySerializedAs("Colour")] [SerializeField] private Color colour = default;
	[SerializeField] private LightSprite objectLightSprite;
	[SerializeField] private CommonComponents commonComponents;

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

	public LightSprite.LightShape SpriteShape;
	public float Size;
	private LightData playerLightData;
	private int lightID;
	[SerializeField] private bool weakenOnGround = false;
	[SerializeField] private bool revertToOldConsistencyBehavior = false;
	[SerializeField, ShowIf(nameof(weakenOnGround))] private float weakenStrength = 1.25f;

	private void Awake()
	{
		if (objectLightEmission == null)
		{
			Logger.LogError($"{this} field objectLightEmission is null, please check {gameObject} prefab.", Category.Lighting);
			return;
		}
		objectLightSprite ??= objectLightEmission.GetComponent<LightSprite>();
		lightID = Guid.NewGuid().GetHashCode();
		playerLightData = new LightData()
		{
			lightColor = colour,
			size = Size,
			lightShape = SpriteShape,
			lightSprite = objectLightSprite.OrNull()?.Sprite,
			Id = lightID,
		};
		LightConsistency();
		commonComponents ??= GetComponent<CommonComponents>();
		commonComponents.RegisterTile.OnAppearClient.AddListener(StateHiddenChange);
		commonComponents.RegisterTile.OnDisappearClient.AddListener(StateHiddenChange);
		commonComponents.UniversalObjectPhysics.OnVisibilityChange += StateHiddenChange;
	}

	private void LightConsistency()
	{
		if (revertToOldConsistencyBehavior) return;
		objectLightSprite.Color = playerLightData.lightColor;
		objectLightSprite.Sprite = playerLightData.lightSprite;
		objectLightSprite.transform.localScale = weakenOnGround ?
			new Vector3(Size / weakenStrength, Size / weakenStrength, Size / weakenStrength)
			: new Vector3(Size, Size, Size);
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//was it transferred from a player's visible inventory?
		if (info.FromPlayer == null && LightEmission != null)
		{
			LightEmission.RemoveLight(playerLightData);
			LightEmission = null;
		}

		if (info.ToPlayer != null)
		{
			if (CompatibleSlots.Contains(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)))
			{
				LightEmission = info.ToPlayer.GetComponent<LightsHolder>();
				if (IsOn == false) return;
				LightEmission.AddLight(playerLightData);
			}
		}
	}

	public void OnThrowDrop()
	{
		if (LightEmission == null) return;
		LightEmission.RemoveLight(playerLightData);
		LightEmission = null;
	}


	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		LightEmission.RemoveLight(playerLightData);
		LightEmission = null;
	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		LightEmission = livingHealth.GetComponent<LightsHolder>();
		if (IsOn == false) return;
		LightEmission.AddLight(playerLightData);
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
		colour = color;
		playerLightData.lightColor = color;
		objectLightSprite.Color = color;
		SetDataOnHolder();
	}

	private void SetDataOnHolder()
	{
		if (LightEmission != null && IsOn)
		{
			LightEmission.Lights[lightID] = playerLightData;
			LightEmission.UpdateLights();
		}
	}

	public void SetSize(int Size)
	{
		this.Size = Size;
		playerLightData.size = Size;
		SetDataOnHolder();
	}

	private void SyncState(bool oldState, bool newState)
	{
		IsOn = newState;
		if (commonComponents.UniversalObjectPhysics.IsVisible == false)
		{
			objectLightEmission.SetActive(newState);
		}
	}


	private void UpdateLights()
	{
		if (IsOn)
		{
			LightEmission.AddLight(playerLightData);
			objectLightEmission.SetActive(true);
		}
		else
		{
			LightEmission.RemoveLight(playerLightData);
			objectLightEmission.SetActive(false);
		}
	}


	private void StateHiddenChange()
	{
		if (commonComponents.UniversalObjectPhysics.IsVisible == false)
		{
			objectLightEmission.SetActive(false);
		}
		else
		{
			objectLightEmission.SetActive(IsOn);
		}
	}
}
