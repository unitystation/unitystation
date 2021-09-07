using Systems.Clothing;
using HealthV2;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// This is used to contain data about the basic rendering of a body part.
/// It will have info about what to render when there's no limb, the position of the rendering, etc.
/// </summary>
public class BodyPartSprites : MonoBehaviour
{
	[SerializeField] public SpriteHandler baseSpriteHandler;

	[SerializeField] public BodyPartType BodyPartType;

	public SpriteRenderer spriteRenderer;

	public CharacterSettings ThisCharacter;

	public SpriteOrder SpriteOrder;

	public ClothingHideFlags ClothingHide;

	public string Data;

	public int referenceOffset = 0;


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
	public virtual void OnDirectionChange(Orientation direction)
	{

		referenceOffset = 0;

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

		baseSpriteHandler.ChangeSpriteVariant(referenceOffset, false);
	}




}
