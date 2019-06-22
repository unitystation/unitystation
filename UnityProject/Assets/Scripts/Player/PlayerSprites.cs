using System;
using System.Collections;
using System.Collections.Generic;
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
	//For character customization
	public ClothingItem[] characterSprites;

	//clothes for each clothing slot
	private readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

	private Directional directional;

	protected void Awake()
	{
		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>())
		{
			clothes[c.name] = c;
		}
	}

	private void OnDirectionChange(Orientation direction)
	{
		//update the clothing sprites
		foreach (ClothingItem c in clothes.Values)
		{
			c.Direction = direction;
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
}