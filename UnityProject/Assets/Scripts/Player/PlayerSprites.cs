using System;
using System.Collections;
using System.Collections.Generic;
using Light2D;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Networking;

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

		//StaticSpriteHandler

		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
		}
		SetupBodySprites();
	

	}

	public void SetupBodySprites() { 
		//Assuming male for now
		clothes["body_torso"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.Torso.Texture != null)
		{clothes["body_torso"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.Torso));}
		else { clothes["body_torso"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.Torso));}
		clothes["body_torso"].spriteHandler.PushTexture();

		clothes["body_rightleg"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.LegRight.Texture != null)
		{clothes["body_rightleg"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.LegRight));}
		else {clothes["body_rightleg"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.LegRight));}
		clothes["body_rightleg"].spriteHandler.PushTexture();


		clothes["body_leftleg"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.LegLeft.Texture != null)
		{clothes["body_leftleg"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.LegLeft));}
		else {clothes["body_leftleg"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.LegLeft));}
		clothes["body_leftleg"].spriteHandler.PushTexture();


		clothes["body_rightarm"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.ArmRight.Texture != null)
		{clothes["body_rightarm"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.ArmRight));}
		else {clothes["body_rightarm"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.ArmRight));}
		clothes["body_rightarm"].spriteHandler.PushTexture();

		Logger.Log(clothes["body_rightarm"].spriteHandler.Infos.List[0].Count.ToString() + " DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");

		clothes["body_leftarm"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.ArmLeft.Texture != null)
		{clothes["body_leftarm"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.ArmLeft));}
		else {clothes["body_leftarm"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.ArmLeft));}
		clothes["body_leftarm"].spriteHandler.PushTexture();


		clothes["body_head"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.Head.Texture != null)
		{clothes["body_head"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.Head));}
		else {clothes["body_head"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.Head));}
		clothes["body_head"].spriteHandler.PushTexture();


		clothes["body_right_hand"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.HandRight.Texture != null)
		{clothes["body_right_hand"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.HandRight));}
		else {clothes["body_right_hand"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.HandRight));}
		clothes["body_right_hand"].spriteHandler.PushTexture();

		clothes["body_left_hand"].spriteHandler.Infos = new SpriteDataForSH();
		if (RaceTexture.Male.HandLeft.Texture != null)
		{clothes["body_left_hand"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Male.HandLeft));}
		else {clothes["body_left_hand"].spriteHandler.Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(RaceTexture.Base.HandLeft));}
		clothes["body_left_hand"].spriteHandler.PushTexture();
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