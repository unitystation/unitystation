using Core.Transforms;
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

	public override void OnDirectionChange(OrientationEnum direction)
	{
		int referenceOffset = 0;

		if (direction == OrientationEnum.Down_By180)
		{
			referenceOffset = 0;
		}

		if (direction == OrientationEnum.Up_By0)
		{
			referenceOffset = 1;
		}

		if (direction == OrientationEnum.Right_By270)
		{
			referenceOffset = 2;
		}

		if (direction == OrientationEnum.Left_By90)
		{
			referenceOffset = 3;
		}

		baseSpriteHandler.ChangeSpriteVariant(referenceOffset);
		OverlySpriteHandler.ChangeSpriteVariant(referenceOffset);
	}
}
