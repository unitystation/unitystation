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
			//default to albino
			for (int i = 0; i < characterSprites.Length; i++)
			{
				characterSprites[i].color = Color.white;
				if (i == 6)
				{
					break;
				}
			}
		}
		else
		{
			//Skintone:
			ColorUtility.TryParseHtmlString(characterSettings.skinTone, out var newColor);

			for (int i = 0; i < characterSprites.Length; i++)
			{
				characterSprites[i].color = newColor;
				if (i == 6)
				{
					break;
				}
			}
			//Torso
			characterSprites[0].reference = characterSettings.torsoSpriteIndex;
			//Head
			characterSprites[5].reference = characterSettings.headSpriteIndex;
			//Eyes
			ColorUtility.TryParseHtmlString(characterSettings.eyeColor, out newColor);
			characterSprites[6].color = newColor;
			//Underwear
			characterSprites[7].reference = characterSettings.underwearOffset;
			//Socks
			characterSprites[8].reference = characterSettings.socksOffset;
			//Beard
			characterSprites[9].reference = characterSettings.facialHairOffset;
			ColorUtility.TryParseHtmlString(characterSettings.facialHairColor, out newColor);
			characterSprites[9].color = newColor;
			//Hair
			characterSprites[10].reference = characterSettings.hairStyleOffset;
			ColorUtility.TryParseHtmlString(characterSettings.hairColor, out newColor);
			characterSprites[10].color = newColor;
		}
	}
}