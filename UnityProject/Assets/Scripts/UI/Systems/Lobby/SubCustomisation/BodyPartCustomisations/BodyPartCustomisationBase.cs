using System.Collections;
using System.Collections.Generic;
using HealthV2;
using TMPro;
using UnityEngine;

namespace UI.CharacterCreator
{
	public class BodyPartCustomisationBase : MonoBehaviour
	{
		public TMP_Text Text;
		public CharacterCustomization characterCustomization;
		public List<SpriteHandlerNorder> RelatedRelatedPreviewSprites
		{
			get
			{
				return characterCustomization.OpenBodySprites[RelatedBodyPart];
			}
		}

		public BodyPart RelatedBodyPart;

		public string pathTo = "";

		public virtual void Deserialise(string InData) { }

		public virtual string Serialise()
		{
			return "";
		}

		/// <summary>
		/// Responsible for setting up body part data from the player's character sheet.
		/// This includes SkinTones, hair and underwear customization, etc.
		/// </summary>
		public virtual void OnPlayerBodyDeserialise(BodyPart Body_Part, string InData, LivingHealthMasterBase livingHealth) { }

		/// <summary>
		/// Responsible for randomizing character customization Works within the body only
		/// </summary>
		public virtual void RandomizeInBody(BodyPart Body_Part, LivingHealthMasterBase livingHealth) { }

		//Randomizes the appearance of this character customisation in the character creator. Works inside the character creator UI only.
		public virtual void RandomizeCharacterCreatorValues() { }

		public virtual void SetUp(CharacterCustomization incharacterCustomization, BodyPart Body_Part, string path)
		{
			RelatedBodyPart = Body_Part;
			characterCustomization = incharacterCustomization;
			Text.text = Body_Part.name;
		}

		/// <summary>
		/// Updates body part customizations inside of the character creator UI.
		/// </summary>
		public virtual void Refresh() { }
	}
}
