using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Lobby;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BodyPartSpriteAndColour : BodyPartCustomisationBase
{
	public Color BodyPartColour = Color.white;
	public Image SelectionColourImage;
	public Dropdown Dropdown;
	public List<SpriteDataSO> OptionalSprites = new List<SpriteDataSO>();

	public struct ColourAndSelected
	{
		public string color;
		public int Chosen;

		public ColourAndSelected(Color Color, int InChosen)
		{
			color = "#" + ColorUtility.ToHtmlStringRGB(Color);
			Chosen = InChosen;
		}
	}

	public override void Deserialise(string InData)
	{
		var ColourAnd_Selected = JsonConvert.DeserializeObject<ColourAndSelected>(InData);

		ColorUtility.TryParseHtmlString(ColourAnd_Selected.color, out BodyPartColour);
		if (ColourAnd_Selected.Chosen >= OptionalSprites.Count)
		{
			Dropdown.value = 0;
		}
		else
		{
			Dropdown.value = ColourAnd_Selected.Chosen;
		}


		Refresh();
	}

	public override string Serialise()
	{
		var Toreturn = new ColourAndSelected(BodyPartColour, Dropdown.value);
		return JsonConvert.SerializeObject(Toreturn);
	}

	public void RequestColourPicker()
	{
		characterCustomization.OpenColorPicker(BodyPartColour, ColorChange, 32f);
	}

	public override void SetUp(CharacterCustomization incharacterCustomization, BodyPart Body_Part, string path)
	{
		base.SetUp(incharacterCustomization, Body_Part, path);
		// Make a list of all available options which can then be passed to the dropdown box
		var itemOptions = OptionalSprites.Select(pcd => pcd.name).ToList();
		itemOptions.Sort();

		// Ensure "None" is at the top of the option lists
		itemOptions.Insert(0, "None");
		Dropdown.AddOptions(itemOptions);
		Dropdown.onValueChanged.AddListener(ItemChange);
	}

	public void ItemChange(int newValue)
	{
		Refresh();
	}

	public override void OnPlayerBodyDeserialise(BodyPart Body_Part, string InData,
		LivingHealthMasterBase LivingHealthMasterBase)
	{
		var ColourAnd_Selected = JsonConvert.DeserializeObject<ColourAndSelected>(InData);
		ColorUtility.TryParseHtmlString(ColourAnd_Selected.color, out BodyPartColour);
		Body_Part.RelatedPresentSprites[0].baseSpriteHandler.SetColor(BodyPartColour);
		OptionalSprites = OptionalSprites.OrderBy(x => x?.name).ToList();
		if (ColourAnd_Selected.Chosen != 0)
		{
			if (ColourAnd_Selected.Chosen >= OptionalSprites.Count)
			{
				Body_Part.RelatedPresentSprites[0].baseSpriteHandler.Empty();
				return;
			}

			Body_Part.RelatedPresentSprites[0].baseSpriteHandler
				.SetSpriteSO(OptionalSprites[ColourAnd_Selected.Chosen - 1]);
		}
		else
		{
			Body_Part.RelatedPresentSprites[0].baseSpriteHandler.Empty();
		}
	}


	private void ColorChange(Color newColor)
	{
		BodyPartColour = newColor;
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

	public override void Refresh()
	{
		//Just the first one for now
		RelatedRelatedPreviewSprites[0].SpriteHandler.SetColor(BodyPartColour);
		SelectionColourImage.color = BodyPartColour;

		if (Dropdown.value == 0)
		{
			RelatedRelatedPreviewSprites[0].SpriteHandler.Empty();
		}
		else
		{
			var ChosenOption = Dropdown.options[Dropdown.value].text;
			var GotOption = OptionalSprites.First(x => x.name == ChosenOption);
			RelatedRelatedPreviewSprites[0].SpriteHandler.SetSpriteSO(GotOption);
		}
	}
}