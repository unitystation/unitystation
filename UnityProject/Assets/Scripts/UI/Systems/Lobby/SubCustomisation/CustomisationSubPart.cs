﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lobby;
using Newtonsoft.Json;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CustomisationSubPart : MonoBehaviour
{
	public TMP_Text HeadName;

	public Dropdown Dropdown;

	public Color color = Color.white;

	public CustomisationType ThisType = CustomisationType.Null;

	public SpriteHandler RelatedSpriteRenderer;

	public CustomisationGroup thisCustomisations;

	public Image SelectionColourImage;

	public CharacterCustomization characterCustomization;

	public SpriteOrder spriteOrder;

	public SpriteHandlerNorder spriteHandlerNorder;

	public void Setup(CustomisationGroup Customisations, CharacterCustomization incharacterCustomization, SpriteOrder _SpriteOrder )
	{
		thisCustomisations = Customisations;
		characterCustomization = incharacterCustomization;
		HeadName.text = Customisations.ThisType.ToString();
		spriteOrder = _SpriteOrder;
		spriteHandlerNorder.SetSpriteOrder(spriteOrder);
		ThisType = Customisations.ThisType;

		// Make a list of all available options which can then be passed to the dropdown box
		var itemOptions = Customisations.PlayerCustomisations.Select(pcd => pcd.Name).ToList();
		itemOptions.Sort();

		// Ensure "None" is at the top of the option lists
		itemOptions.Insert(0, "None");
		Dropdown.AddOptions(itemOptions);

		RelatedSpriteRenderer.gameObject.transform.SetParent(incharacterCustomization.SpriteContainer.transform);
		RelatedSpriteRenderer.gameObject.transform.localPosition = Vector3.zero;
		Dropdown.onValueChanged.AddListener(ItemChange);
	}

	public CharacterSettings.CustomisationClass Serialise()
	{
		var newcurrentSetting = new CharacterSettings.CustomisationClass();
		// if (thisCustomisations.CanColour)
		// {
			newcurrentSetting.Colour = "#" + ColorUtility.ToHtmlStringRGB(color);
		// }
		newcurrentSetting.SelectedName = Dropdown.options[Dropdown.value].text;


		return newcurrentSetting;

	}


	public void SetDropdownValue(CharacterSettings.CustomisationClass currentSetting)
	{
		// Find the index of the setting in the dropdown list which matches the currentSetting
		int settingIndex = Dropdown.options.FindIndex(option => option.text == currentSetting.SelectedName);

		if (settingIndex != -1)
		{
			// Make sure FindIndex is successful before changing value
			Dropdown.value = settingIndex;
		}
		else
		{
			Logger.LogWarning($"Unable to find index of {currentSetting}! Using default", Category.Character);
			Dropdown.value = 0;
		}

		// if (thisCustomisations.CanColour)
		// {
			//currentCharacter.HairColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentSetting.Colour, out setColor);
			color = setColor;
			SelectionColourImage.color = setColor;
			RelatedSpriteRenderer.SetColor(setColor);
		// }

		Refresh();

	}


	public void DropdownScrollRight()
	{
		// Check if value should wrap around
		if (Dropdown.value < Dropdown.options.Count - 1)
		{
			Dropdown.value++;
		}
		else
		{
			Dropdown.value = 0;
		}

		//Refresh();
	}

	public void DropdownScrollLeft()
	{
		// Check if value should wrap around
		if (Dropdown.value > 0)
		{
			Dropdown.value--;
		}
		else
		{
			Dropdown.value = Dropdown.options.Count - 1;
		}
//Refresh();
	}

	public void SetRotation(int newValue)
	{
		RelatedSpriteRenderer.ChangeSpriteVariant(newValue);
	}

	public void ItemChange(int newValue)
	{
		Refresh();
	}

	public void RequestColourPicker()
	{
		characterCustomization.OpenColorPicker(color, ColorChange, 32f);
	}

	private void ColorChange(Color newColor)
	{
		color = newColor;
		Refresh();
	}
	public void Refresh()
	{
		if (Dropdown.value == 0)
		{
			RelatedSpriteRenderer.Empty();
		}
		else
		{
			var ChosenOption = Dropdown.options[Dropdown.value].text;
			var GotOption = thisCustomisations.PlayerCustomisations.First(x => x.Name == ChosenOption);
			RelatedSpriteRenderer.SetSpriteSO(GotOption.SpriteEquipped);
		}

		//set Selection and sprite
		RelatedSpriteRenderer.SetColor(color);
		SelectionColourImage.color = color;
	}
}
