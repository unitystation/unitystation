using Newtonsoft.Json;
using UnityEngine;
using HealthV2;
using Systems.Character;
using Systems.Clothing;

/// <summary>
/// This is used to contain data about the basic rendering of a body part.
/// It will have info about what to render when there's no limb, the position of the rendering, etc.
/// </summary>
public class BodyPartSprites : MonoBehaviour
{
	[SerializeField] public SpriteHandler baseSpriteHandler;

	[SerializeField] public BodyPartType BodyPartType;

	public SpriteRenderer spriteRenderer;

	public SpriteOrder SpriteOrder;

	public ClothingHideFlags ClothingHide;

	public string Data;

	public int referenceOffset = 0;

	public IntName intName;

	public void UpdateData(string InNew)
	{
		if (string.IsNullOrEmpty(InNew)) return;
		Data = InNew;
		SpriteOrder = JsonConvert.DeserializeObject<SpriteOrder>(Data);
		SpriteOrder.Orders.RemoveRange(0, 4);
		if (SpriteOrder != null)
		{
			if (SpriteOrder.Orders.Count > referenceOffset)
			{
				spriteRenderer.sortingOrder = SpriteOrder.Orders[referenceOffset];
			}
		}
		if (baseSpriteHandler == null) return;
		baseSpriteHandler.ChangeSpriteVariant(referenceOffset, false);
	}

	public virtual void UpdateSpritesForImplant(BodyPart implant,ClothingHideFlags INClothingHide, SpriteDataSO Sprite, SpriteOrder _SpriteOrder = null)
	{
		if (baseSpriteHandler == null) return;
		ClothingHide = INClothingHide;
		UpdateData( JsonConvert.SerializeObject(_SpriteOrder));
		//baseSpriteHandler.name = baseSpriteHandler.name + implant.name;
		baseSpriteHandler.SetSpriteSO(Sprite, Color.white);
		SpriteOrder = _SpriteOrder;
		if (SpriteOrder != null)
		{
			if (SpriteOrder.Orders.Count > 0)
			{
				spriteRenderer.sortingOrder = SpriteOrder.Orders[0];
			}
		}
		baseSpriteHandler.ChangeSpriteVariant(referenceOffset, false);
	}


	public virtual void SetName(string Name)
	{
		gameObject.name = Name;
		baseSpriteHandler.name = Name;
	}
	public virtual void OnDirectionChange(OrientationEnum direction)
	{

		referenceOffset = 0;

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

		if (SpriteOrder != null)
		{
			if (SpriteOrder.Orders.Count > referenceOffset)
			{
				spriteRenderer.sortingOrder = SpriteOrder.Orders[referenceOffset];
			}
		}

		//Not networked so don't run sprite change on headless
		if (CustomNetworkManager.IsHeadless) return;

		baseSpriteHandler.ChangeSpriteVariant(referenceOffset, false);
	}




}
