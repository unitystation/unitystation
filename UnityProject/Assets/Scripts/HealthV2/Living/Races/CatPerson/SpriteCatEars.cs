using System.Collections;
using System.Collections.Generic;
using Systems.Clothing;
using UnityEngine;
using HealthV2;
public class SpriteCatEars : BodyPartSprites
{

	[SerializeField] protected SpriteHandler OverlySpriteHandler;
	public override void UpdateSpritesForImplant(BodyPart implant,ClothingHideFlags INClothingHide, SpriteDataSO Sprite, SpriteOrder _SpriteOrder = null)
	{
		ClothingHide = INClothingHide;
		SpriteOrder = _SpriteOrder;
		baseSpriteHandler.PushTexture();
		OverlySpriteHandler.PushTexture();
	}

	public override void SetName(string Name)
	{
		this.gameObject.name = Name;
		baseSpriteHandler.name = Name;
		OverlySpriteHandler.name = Name + "_" + "Overly";
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
