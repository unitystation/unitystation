using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HealthV2;
public class SpriteCatEars : BodyPartSprites
{

	[SerializeField] protected SpriteHandler OverlySpriteHandler;
	public override void UpdateSpritesForImplant(BodyPart implant, SpriteDataSO Sprite, RootBodyPartContainer rootBodyPartContainer, SpriteOrder _SpriteOrder = null)
	{
		SpriteOrder = _SpriteOrder;
		baseSpriteHandler.PushTexture();
		// if (ColorUtility.TryParseHtmlString(rootBodyPartContainer.PlayerSprites.ThisCharacter.HairColor, out var newColor))
		// {
			// baseSpriteHandler.SetColor(newColor);
		// }

		OverlySpriteHandler.PushTexture();
	}

	public override void OnDirectionChange(Orientation direction)
	{
		int referenceOffset = 0;

		if (direction == Orientation.Down)
		{
			referenceOffset = 0;
		}

		if (direction == Orientation.Up)
		{
			referenceOffset = 1;
		}

		if (direction == Orientation.Right)
		{
			referenceOffset = 2;
		}

		if (direction == Orientation.Left)
		{
			referenceOffset = 3;
		}
		baseSpriteHandler.ChangeSpriteVariant(referenceOffset);
		OverlySpriteHandler.ChangeSpriteVariant(referenceOffset);
	}
}
