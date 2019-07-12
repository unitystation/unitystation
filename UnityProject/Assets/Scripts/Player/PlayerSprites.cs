using System;
using System.Collections;
using System.Collections.Generic;
using Light2D;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handle displaying the sprites related to player, which includes clothing and the body.
/// Ghosts are handled in GhostSprites.
/// </summary>
[RequireComponent(typeof(Directional))]
[RequireComponent(typeof(PlayerScript))]
public class PlayerSprites : MonoBehaviour
{
	private static GameObject ENGULFED_BURNING_OVERLAY_PREFAB;
	private static GameObject PARTIAL_BURNING_OVERLAY_PREFAB;

	/// <summary>
	/// Threshold value where we switch from partial burning to fully engulfed sprite.
	/// </summary>
	private static readonly float FIRE_STACK_ENGULF_THRESHOLD = 3;

	//For character customization
	public ClothingItem[] characterSprites;

	//clothes for each clothing slot
	private readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

	private Directional directional;
	private BurningDirectionalOverlay engulfedBurningOverlay;
	private BurningDirectionalOverlay partialBurningOverlay;
	private LivingHealthBehaviour livingHealthBehaviour;
	private PlayerScript playerScript;
	private PlayerHealth playerHealth;
	private PlayerSync playerSync;
	[Tooltip("Muzzle flash, should be on a child of the player gameobject")]
	public LightSprite muzzleFlash;

	protected void Awake()
	{
		if (ENGULFED_BURNING_OVERLAY_PREFAB == null)
		{
			ENGULFED_BURNING_OVERLAY_PREFAB = Resources.Load<GameObject>("EngulfedBurningPlayer");
			PARTIAL_BURNING_OVERLAY_PREFAB = Resources.Load<GameObject>("PartialBurningPlayer");
		}

		if (engulfedBurningOverlay == null)
		{
			engulfedBurningOverlay = GameObject.Instantiate(ENGULFED_BURNING_OVERLAY_PREFAB, transform)
				.GetComponent<BurningDirectionalOverlay>();
			engulfedBurningOverlay.enabled = true;
			engulfedBurningOverlay.StopBurning();
			partialBurningOverlay = GameObject.Instantiate(PARTIAL_BURNING_OVERLAY_PREFAB, transform)
				.GetComponent<BurningDirectionalOverlay>();
			partialBurningOverlay.enabled = true;
			partialBurningOverlay.StopBurning();
		}

		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
		livingHealthBehaviour.OnClientFireStacksChange.AddListener(OnClientFireStacksChange);
		OnClientFireStacksChange(livingHealthBehaviour.FireStacks);


		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
		}
	}

	private void OnClientFireStacksChange(float newStacks)
	{
		if (newStacks <= 0)
		{
			engulfedBurningOverlay.StopBurning();
			partialBurningOverlay.StopBurning();
		}
		else
		{
			if (newStacks >= FIRE_STACK_ENGULF_THRESHOLD)
			{
				engulfedBurningOverlay.Burn(directional.CurrentDirection);
				partialBurningOverlay.StopBurning();
			}
			else
			{
				partialBurningOverlay.Burn(directional.CurrentDirection);
				engulfedBurningOverlay.StopBurning();
			}
		}
	}

	private void OnDirectionChange(Orientation direction)
	{
		//update the clothing sprites
		foreach (ClothingItem c in clothes.Values)
		{
			c.Direction = direction;
		}

		if (livingHealthBehaviour.FireStacks > 0)
		{
			if (livingHealthBehaviour.FireStacks >= FIRE_STACK_ENGULF_THRESHOLD)
			{
				engulfedBurningOverlay.Burn(direction);
			}
			else
			{
				partialBurningOverlay.Burn(direction);
			}
		}
	}

	public void NotifyPlayer(GameObject recipient)
	{
		for (int i = 0; i < characterSprites.Length; i++)
		{
			var clothItem = characterSprites[i];
			PlayerSpritesMessage.SendTo(gameObject, i, clothItem.reference, clothItem.color, recipient);
		}
	}

	public void OnCharacterSettingsChange(CharacterSettings characterSettings)
	{
		if (characterSettings == null)
		{
			characterSettings = new CharacterSettings();
		}

		//Skintone:
		ColorUtility.TryParseHtmlString(characterSettings.skinTone, out var newColor);


		//Torso
		PlayerSpritesMessage.SendToAll(gameObject, 0, characterSettings.torsoSpriteIndex, newColor);
		//right leg
		PlayerSpritesMessage.SendToAll(gameObject, 1, characterSettings.rightLegSpriteIndex, newColor);
		//left leg
		PlayerSpritesMessage.SendToAll(gameObject, 2, characterSettings.leftLegSpriteIndex, newColor);
		//right arm
		PlayerSpritesMessage.SendToAll(gameObject, 3, characterSettings.rightArmSpriteIndex, newColor);
		//left arm
		PlayerSpritesMessage.SendToAll(gameObject, 4, characterSettings.leftArmSpriteIndex, newColor);
		//Head
		PlayerSpritesMessage.SendToAll(gameObject, 5, characterSettings.headSpriteIndex, newColor);
		//Eyes
		ColorUtility.TryParseHtmlString(characterSettings.eyeColor, out newColor);
		PlayerSpritesMessage.SendToAll(gameObject, 6, 1, newColor);
		//Underwear
		PlayerSpritesMessage.SendToAll(gameObject, 7, characterSettings.underwearOffset, Color.white);
		//Socks
		PlayerSpritesMessage.SendToAll(gameObject, 8, characterSettings.socksOffset, Color.white);
		//Beard
		ColorUtility.TryParseHtmlString(characterSettings.facialHairColor, out newColor);
		PlayerSpritesMessage.SendToAll(gameObject, 9, characterSettings.facialHairOffset, newColor);
		//Hair
		ColorUtility.TryParseHtmlString(characterSettings.hairColor, out newColor);
		PlayerSpritesMessage.SendToAll(gameObject, 10, characterSettings.hairStyleOffset, newColor);
	}


	/// <summary>
	/// Display the muzzle flash animation
	/// </summary>
	public void ShowMuzzleFlash()
	{
		StartCoroutine(AnimateMuzzleFlash());
	}

	private IEnumerator AnimateMuzzleFlash()
	{
		muzzleFlash.gameObject.SetActive(true);
		yield return WaitFor.Seconds(0.1f);
		muzzleFlash.gameObject.SetActive(false);
	}
}