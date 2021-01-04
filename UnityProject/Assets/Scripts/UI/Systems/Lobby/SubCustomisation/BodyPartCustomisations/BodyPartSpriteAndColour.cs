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


    public TMP_Text HeadName;

    public Dropdown Dropdown;

    public CustomisationGroup thisCustomisations;


    public List<SpriteDataSO>  OptionalSprites = new List<SpriteDataSO>();

    public struct ColourAndSelected
    {
	    public string color;
	    public string Chosen;

    }

    public override void Deserialise(string InData)
    {
		var ColourAnd_Selected = JsonConvert.DeserializeObject<ColourAndSelected>(InData);

	    ColorUtility.TryParseHtmlString(ColourAnd_Selected.color, out BodyPartColour);



	    Refresh();
    }

    public override string Serialise()
    {
	    return "#" + ColorUtility.ToHtmlStringRGB(BodyPartColour);

	    // Make a list of all available options which can then be passed to the dropdown box
	    var itemOptions = OptionalSprites.Select(pcd => pcd.name).ToList();
	    itemOptions.Sort();

	    // Ensure "None" is at the top of the option lists
	    itemOptions.Insert(0, "None");
	    Dropdown.AddOptions(itemOptions);

    }

    public void RequestColourPicker()
    {
	    characterCustomization.OpenColorPicker(BodyPartColour, ColorChange, 32f);
    }

    public override void SetUp(CharacterCustomization incharacterCustomization, BodyPart Body_Part, string path)
    {
	    base.SetUp(incharacterCustomization, Body_Part, path);


    }

    public override void OnPlayerBodyDeserialise(BodyPart Body_Part, string InData, LivingHealthMasterBase LivingHealthMasterBase)
    {
	    ColorUtility.TryParseHtmlString(InData, out BodyPartColour);
	    Body_Part.RelatedPresentSprites[0].baseSpriteHandler.SetColor(BodyPartColour);
    }


    private void ColorChange(Color newColor)
    {
	    BodyPartColour = newColor;
	    Refresh();
    }

    public override void Refresh()
    {
	    //Just the first one for now
	    RelatedRelatedPreviewSprites[0].SpriteHandler.SetColor(BodyPartColour);
	    SelectionColourImage.color = BodyPartColour;
    }
}
