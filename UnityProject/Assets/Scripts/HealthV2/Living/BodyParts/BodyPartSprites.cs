using Systems.Clothing;
using HealthV2;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// This is used to contain data about the basic rendering of a body part.
/// It will have info about what to render when there's no limb, the position of the rendering, etc.
/// </summary>
public class BodyPartSprites : NetworkBehaviour
{
	[SerializeField] public SpriteHandler baseSpriteHandler;

	[SerializeField] public BodyPartType BodyPartType;

	public SpriteRenderer spriteRenderer;

	public CharacterSettings ThisCharacter;

	public SpriteOrder SpriteOrder;

	public ClothingHideFlags ClothingHide;

	[SyncVar(hook = nameof(UpdateData))]
	private string Data;

	public void UpdateData(string InOld, string InNew)
	{
		Data = InNew;
		if (CustomNetworkManager.Instance._isServer) return;
		SpriteOrder = JsonConvert.DeserializeObject<SpriteOrder>(Data);
		SpriteOrder.Orders.RemoveRange(0, 4);
	}

	public virtual void UpdateSpritesForImplant(BodyPart implant,ClothingHideFlags INClothingHide, SpriteDataSO Sprite, RootBodyPartContainer rootBodyPartContainer, SpriteOrder _SpriteOrder = null)
	{
		if (baseSpriteHandler == null) return;
		ClothingHide = INClothingHide;
		UpdateData("", JsonConvert.SerializeObject(_SpriteOrder));
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
