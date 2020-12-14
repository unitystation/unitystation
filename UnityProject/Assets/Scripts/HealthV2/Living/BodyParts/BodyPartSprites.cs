using HealthV2;
using UnityEngine;

/// <summary>
/// This is used to contain data about the basic rendering of a body part.
/// It will have info about what to render when there's no limb, the position of the rendering, etc.
/// </summary>
public class BodyPartSprites : MonoBehaviour
{
	[SerializeField] protected SpriteHandler baseSpriteHandler;

	[SerializeField] private SpriteHandler damageOverlaySpriteHandler;

	[SerializeField] public BodyPartType bodyPartType;

	public CharacterSettings ThisCharacter;



	public virtual void UpdateSpritesForImplant(BodyPart implant, SpriteDataSO Sprite, RootBodyPartContainer rootBodyPartContainer)
	{
		//TODOH Colour
		if (baseSpriteHandler == null) return;
		//Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f)
		baseSpriteHandler.SetSpriteSO(Sprite, Color.white);
	}

	public virtual void OnDirectionChange(Orientation direction)
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
	}




}
