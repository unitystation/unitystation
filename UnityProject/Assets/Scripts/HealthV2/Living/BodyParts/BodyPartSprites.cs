using HealthV2;
using UnityEngine;

/// <summary>
/// This is used to contain data about the basic rendering of a body part.
/// It will have info about what to render when there's no limb, the position of the rendering, etc.
/// </summary>
public class BodyPartSprites : MonoBehaviour
{
	[SerializeField] public SpriteHandler baseSpriteHandler;

	[SerializeField] private SpriteHandler damageOverlaySpriteHandler;

	[SerializeField] public BodyPartType bodyPartType;

	public SpriteRenderer spriteRenderer;

	public CharacterSettings ThisCharacter;

	public SpriteOrder SpriteOrder;



	public virtual void UpdateSpritesForImplant(BodyPart implant, SpriteDataSO Sprite, RootBodyPartContainer rootBodyPartContainer, SpriteOrder _SpriteOrder = null)
	{
		//TODOH Colour
		if (baseSpriteHandler == null) return;
		//Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f)
		baseSpriteHandler.SetSpriteSO(Sprite, Color.white);
		SpriteOrder = _SpriteOrder;
		if (SpriteOrder != null)
		{
			if (SpriteOrder.Orders.Count > 0)
			{
				spriteRenderer.sortingOrder = SpriteOrder.Orders[0];
			}
		}
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

		if (SpriteOrder != null)
		{
			if (SpriteOrder.Orders.Count > referenceOffset)
			{
				spriteRenderer.sortingOrder = SpriteOrder.Orders[referenceOffset];
			}
		}

		baseSpriteHandler.ChangeSpriteVariant(referenceOffset);
	}




}
