using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CharacterCreator
{
	public class BodyPartColourSprite : BodyPartCustomisationBase
	{
		public Color BodyPartColour = Color.white;
		public Image SelectionColourImage;

		public bool AppliesToItemsSprite = false;

		public override void Deserialise(string InData)
		{
			ColorUtility.TryParseHtmlString(InData, out BodyPartColour);
			BodyPartColour.a = 1;
			Refresh();
		}

		public override string Serialise()
		{
			BodyPartColour.a = 1;
			return "#" + ColorUtility.ToHtmlStringRGB(BodyPartColour);
		}

		public void RequestColourPicker()
		{
			characterCustomization.OpenColorPicker(BodyPartColour, ColorChange, 32f);
		}

		public override void OnPlayerBodyDeserialise(BodyPart Body_Part, string InData, LivingHealthMasterBase livingHealth)
		{
			Body_Part.SetCustomisationData = InData;
			ColorUtility.TryParseHtmlString(InData, out BodyPartColour);
			SetupBody(Body_Part, BodyPartColour);
		}

		public void SetupBody(BodyPart Body_Part, Color InData)
		{
			BodyPartColour = InData;
			BodyPartColour.a = 1; //Force body part color to never be transparent.
			Body_Part.RelatedPresentSprites[0].baseSpriteHandler.SetColor(BodyPartColour);
			if (AppliesToItemsSprite)
			{
				Body_Part.BodyPartItemSprite.SetColor(BodyPartColour);
			}

		}

		public override void RandomizeInBody(BodyPart Body_Part, LivingHealthMasterBase livingHealth)
		{
			SetupBody(Body_Part, new Color(Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), 1f));
		}

		public override void RandomizeCharacterCreatorValues()
		{
			ColorChange(new Color(Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), 1f));
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
}
