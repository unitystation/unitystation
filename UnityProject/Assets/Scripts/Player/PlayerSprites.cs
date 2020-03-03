﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using NUnit.Framework.Constraints;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

/// <summary>
/// Handle displaying the sprites related to player, which includes underwear and the body.
/// Note that the clothing you put on (UniCloths) are handled in Equipment
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

	public PlayerTextureData RaceTexture;

	//For character customization
	public ClothingItem[] characterSprites;

	public CharacterSettings ThisCharacter;

	//clothes for each clothing slot
	//TODO: don't use string as the dictionary key
	public readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

	private Directional directional;
	private BurningDirectionalOverlay engulfedBurningOverlay;
	private BurningDirectionalOverlay partialBurningOverlay;
	private LivingHealthBehaviour livingHealthBehaviour;
	private PlayerScript playerScript;
	private PlayerHealth playerHealth;
	private PlayerSync playerSync;

	private ClothingHideFlags hideClothingFlags = ClothingHideFlags.HIDE_NONE;
	/// <summary>
	/// Define which piece of clothing are hidden (not rendering) right now
	/// </summary>
	public ClothingHideFlags HideClothingFlags => hideClothingFlags;

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

		//StaticSpriteHandler

		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
			// add listner in case clothing was changed
			c.OnClothingEquiped += OnClothingEquipped;
		}

		SetupBodySprites();
	}

	public void SetupCharacterData(CharacterSettings Character)
	{
		ThisCharacter = Character;
		RaceTexture = Spawn.RaceData["human"];
		SetupBodySprites();
		SetupCustomisations();
		OnDirectionChange(directional.CurrentDirection);

	}

	public void SetupCustomisations()
	{
		if (ThisCharacter.underwearName != "None")
		{
			clothes["underwear"].spriteHandler.spriteData = new SpriteData();
			clothes["underwear"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				Spawn.PlayerCustomisationData[
					PlayerCustomisation.Underwear][ThisCharacter.underwearName].Equipped));
			clothes["underwear"].spriteHandler.PushTexture();
		}

		if (ThisCharacter.socksName != "None")
		{
			clothes["socks"].spriteHandler.spriteData = new SpriteData();
			clothes["socks"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				Spawn.PlayerCustomisationData[
					PlayerCustomisation.Socks][ThisCharacter.socksName].Equipped));
			clothes["socks"].spriteHandler.PushTexture();
		}


		if (ThisCharacter.facialHairName != "None")
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.facialHairColor, out var newColor);
			clothes["beard"].spriteHandler.spriteData = new SpriteData();
			clothes["beard"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				Spawn.PlayerCustomisationData[
					PlayerCustomisation.FacialHair][ThisCharacter.facialHairName].Equipped));
			clothes["beard"].spriteHandler.SetColor(newColor);
			clothes["beard"].spriteHandler.PushTexture();
		}

		if (ThisCharacter.hairStyleName != "None")
		{
			ColorUtility.TryParseHtmlString(ThisCharacter.hairColor, out var newColor);
			clothes["Hair"].spriteHandler.spriteData = new SpriteData();
			clothes["Hair"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(
				Spawn.PlayerCustomisationData[
					PlayerCustomisation.HairStyle][ThisCharacter.hairStyleName].Equipped));
			clothes["Hair"].spriteHandler.SetColor(newColor);
			clothes["Hair"].spriteHandler.PushTexture();
		}
	}

	public void SetupBodySprites()
	{
		//RaceVariantTextureData
		//Assuming male for now
		SexSetupBodySprites(RaceTexture.Base);
		if (ThisCharacter.Gender == Gender.Female)
		{
			SexSetupBodySprites(RaceTexture.Female);
		}
		else
		{
			SexSetupBodySprites(RaceTexture.Male);
		}
	}


	public void SexSetupBodySprites(RaceVariantTextureData Variant)
	{
		ColorUtility.TryParseHtmlString(ThisCharacter.skinTone, out var newColor);

		if (Variant.Torso.Texture != null)
		{
			clothes["body_torso"].spriteHandler.spriteData = new SpriteData();
			clothes["body_torso"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(Variant.Torso));
			clothes["body_torso"].spriteHandler.SetColor(newColor);
			clothes["body_torso"].spriteHandler.PushTexture();
		}


		if (Variant.LegRight.Texture != null)
		{
			clothes["body_rightleg"].spriteHandler.spriteData = new SpriteData();
			clothes["body_rightleg"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.LegRight));
			clothes["body_rightleg"].spriteHandler.SetColor(newColor);
			clothes["body_rightleg"].spriteHandler.PushTexture();
		}


		if (Variant.LegLeft.Texture != null)
		{
			clothes["body_leftleg"].spriteHandler.spriteData = new SpriteData();
			clothes["body_leftleg"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.LegLeft));
			clothes["body_leftleg"].spriteHandler.SetColor(newColor);
			clothes["body_leftleg"].spriteHandler.PushTexture();
		}

		if (Variant.ArmRight.Texture != null)
		{
			clothes["body_rightarm"].spriteHandler.spriteData = new SpriteData();
			clothes["body_rightarm"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.ArmRight));
			clothes["body_rightarm"].spriteHandler.SetColor(newColor);
			clothes["body_rightarm"].spriteHandler.PushTexture();
		}


		if (Variant.ArmLeft.Texture != null)
		{
			clothes["body_leftarm"].spriteHandler.spriteData = new SpriteData();
			clothes["body_leftarm"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.ArmLeft));
			clothes["body_leftarm"].spriteHandler.SetColor(newColor);
			clothes["body_leftarm"].spriteHandler.PushTexture();
		}


		if (Variant.Head.Texture != null)
		{
			clothes["body_head"].spriteHandler.spriteData = new SpriteData();
			clothes["body_head"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(Variant.Head));
			clothes["body_head"].spriteHandler.SetColor(newColor);
			clothes["body_head"].spriteHandler.PushTexture();
		}


		if (Variant.HandRight.Texture != null)
		{
			clothes["body_right_hand"].spriteHandler.spriteData = new SpriteData();
			clothes["body_right_hand"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.HandRight));
			clothes["body_right_hand"].spriteHandler.SetColor(newColor);
			clothes["body_right_hand"].spriteHandler.PushTexture();
		}


		if (Variant.HandLeft.Texture != null)
		{
			clothes["body_left_hand"].spriteHandler.spriteData = new SpriteData();
			clothes["body_left_hand"].spriteHandler.spriteData.List
				.Add(SpriteFunctions.CompleteSpriteSetup(Variant.HandLeft));
			clothes["body_left_hand"].spriteHandler.SetColor(newColor);
			clothes["body_left_hand"].spriteHandler.PushTexture();
		}

		ColorUtility.TryParseHtmlString(ThisCharacter.eyeColor, out newColor);
		if (Variant.Eyes.Texture != null)
		{
			clothes["eyes"].spriteHandler.spriteData = new SpriteData();
			clothes["eyes"].spriteHandler.spriteData.List.Add(SpriteFunctions.CompleteSpriteSetup(Variant.Eyes));
			clothes["eyes"].spriteHandler.SetColor(newColor);
			clothes["eyes"].spriteHandler.PushTexture();
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

	public void NotifyPlayer(GameObject recipient, bool clothItems = false)
	{
		if (!clothItems)
		{
			PlayerCustomisationMessage.SendTo(gameObject, recipient, ThisCharacter);
		}
		else
		{
			for (int i = 0; i < characterSprites.Length; i++)
			{
				var clothItem = characterSprites[i];
				PlayerAppearanceMessage.SendTo(gameObject, i, recipient, clothItem.GameObjectReference, true, true);
			}
		}
	}

	public void OnCharacterSettingsChange(CharacterSettings characterSettings)
	{
		if (characterSettings == null)
		{
			characterSettings = new CharacterSettings();
		}

		SetupCharacterData(characterSettings);
		//Torso
		PlayerCustomisationMessage.SendToAll(gameObject, characterSettings);
		//right leg
		//PlayerCustomisationMessage.SendToAll(gameObject, "body_rightleg", characterSettings.rightLegSpriteIndex, newColor, characterSettings);
		////left leg
		//PlayerCustomisationMessage.SendToAll(gameObject, "body_leftleg", characterSettings.leftLegSpriteIndex, newColor, characterSettings);
		////right arm
		//PlayerCustomisationMessage.SendToAll(gameObject, "body_rightarm", characterSettings.rightArmSpriteIndex, newColor, characterSettings);
		////left arm
		//PlayerCustomisationMessage.SendToAll(gameObject, "body_leftarm", characterSettings.leftArmSpriteIndex, newColor, characterSettings);
		////Head
		//PlayerCustomisationMessage.SendToAll(gameObject, "body_head", characterSettings.headSpriteIndex, newColor, characterSettings);


		//PlayerCustomisationMessage.SendToAll(gameObject, "body_right_hand", characterSettings.headSpriteIndex, newColor, characterSettings);
		//PlayerCustomisationMessage.SendToAll(gameObject, "body_left_hand", characterSettings.headSpriteIndex, newColor, characterSettings);

		//Eyes
		//ColorUtility.TryParseHtmlString(characterSettings.eyeColor, out newColor);
		//PlayerCustomisationMessage.SendToAll(gameObject, "eyes", 1, newColor);
		////Underwear
		//PlayerCustomisationMessage.SendToAll(gameObject, "underwear", characterSettings.underwearOffset, Color.white);
		////Socks
		//PlayerCustomisationMessage.SendToAll(gameObject, "socks", characterSettings.socksOffset, Color.white);
		////Beard
		//ColorUtility.TryParseHtmlString(characterSettings.facialHairColor, out newColor);
		//PlayerCustomisationMessage.SendToAll(gameObject, "beard", characterSettings.facialHairOffset, newColor);
		////Hair
		//ColorUtility.TryParseHtmlString(characterSettings.hairColor, out newColor);
		//PlayerCustomisationMessage.SendToAll(gameObject, "Hair", characterSettings.hairStyleOffset, newColor);
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

	/// <summary>
	/// Returns true iff this playersprites has a clothing item for the specified named slot
	/// </summary>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public bool HasClothingItem(NamedSlot? namedSlot)
	{
		return characterSprites.FirstOrDefault(ci => ci.Slot == namedSlot) != null;
	}


	private void OnClothingEquipped(ClothingV2 clothing, bool isEquiped)
	{
		//Logger.Log($"Clothing {clothing} was equipped {isEquiped}!", Category.Inventory);

		// if new clothes equiped, add new hide flags
		if (isEquiped)
			hideClothingFlags |= clothing.HideClothingFlags;
		// if player get off old clothes, we need to remove old flags
		else
			hideClothingFlags ^= clothing.HideClothingFlags;

		// Update hide flags
		ValidateHideFlags();
	}

	private void ValidateHideFlags()
	{
		// Need to check all flags with their gameobject names...
		// TODO: it should be done much easier
		ValidateHideFlag(ClothingHideFlags.HIDE_GLOVES, "hands");
		ValidateHideFlag(ClothingHideFlags.HIDE_JUMPSUIT, "uniform");
		ValidateHideFlag(ClothingHideFlags.HIDE_SHOES, "feet");
		ValidateHideFlag(ClothingHideFlags.HIDE_MASK, "mask");
		ValidateHideFlag(ClothingHideFlags.HIDE_EARS, "ear");
		ValidateHideFlag(ClothingHideFlags.HIDE_EYES, "eyes");
		ValidateHideFlag(ClothingHideFlags.HIDE_FACE, "face");
		ValidateHideFlag(ClothingHideFlags.HIDE_HAIR, "Hair");
		ValidateHideFlag(ClothingHideFlags.HIDE_FACIALHAIR, "beard");
		ValidateHideFlag(ClothingHideFlags.HIDE_NECK, "neck");

		// TODO: Not implemented yet?
		//ValidateHideFlag(ClothingHideFlags.HIDE_SUITSTORAGE, "suit_storage");
	}

	private void ValidateHideFlag(ClothingHideFlags hideFlag, string name)
	{
		// Check if dictionary has entry about such clothing item name
		if (!clothes.ContainsKey(name))
		{
			Logger.LogError($"Can't find {name} clothingItem linked to {hideFlag}");
			return;
		}

		// Enable or disable based on hide flag
		var isVisible = !hideClothingFlags.HasFlag(hideFlag);
		clothes[name].gameObject.SetActive(isVisible);
	}

}

public enum ClothingSprite
{
	body_torso,
	body_rightleg,
	body_leftleg,
	body_rightarm,
	body_leftarm,
	body_head,
	body_right_hand,
	body_left_hand,
	eyes,
	underwear,
	socks,
	beard,
	Hair,
}