using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Lobby;
using TMPro;
using UnityEngine;

public class BodyPartCustomisationBase : MonoBehaviour
{
	public TMP_Text Text;
	public CharacterCustomization characterCustomization;
	public List<SpriteHandlerNorder> RelatedRelatedPreviewSprites => characterCustomization.OpenBodySprites[RelatedBodyPart];

	public BodyPart RelatedBodyPart;

	public string pathTo = "";

	public virtual void Deserialise(string InData)
	{

	}



	public virtual string Serialise()
	{
		return "";
	}


	public virtual void OnPlayerBodyDeserialise(BodyPart Body_Part, string InData, LivingHealthMasterBase LivingHealthMasterBase)
	{

	}



	public virtual void SetUp(CharacterCustomization incharacterCustomization, BodyPart Body_Part, string path)
	{
		RelatedBodyPart = Body_Part;
		characterCustomization = incharacterCustomization;
		Text.text = Body_Part.name;
	}

	public virtual void Refresh()
	{
	}
}
