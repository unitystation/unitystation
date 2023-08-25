using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

public class SpriteHandlerNorder : MonoBehaviour
{
	public SpriteHandler SpriteHandler;

	[SerializeField]
	private SpriteOrder spriteOrder  = new SpriteOrder();

	public SpriteOrder SpriteOrder  => spriteOrder;

	public SpriteRenderer spriteRenderer;

	private string Data;


	public void SetSpriteOrder(SpriteOrder InSpriteOrder, bool Lobby = false)
	{
		spriteOrder = InSpriteOrder;
		if (Lobby == false)
		{
			UpdateData(JsonConvert.SerializeObject(spriteOrder));
		}
	}

	public void UpdateData(string InNew)
	{
		if (InNew == null)
		{
			SpriteHandler.ClearPresentSpriteSet();
			spriteRenderer.sprite = null;
			return;
		}

		Data = InNew;
		if (CustomNetworkManager.Instance._isServer) return;
		spriteOrder = JsonConvert.DeserializeObject<SpriteOrder>(Data);
		SpriteOrder.Orders.RemoveRange(0, 4);
	}

	public void ChangeSpriteVariant(int number)
	{
		SpriteHandler.ChangeSpriteVariant(number);
	}

	public virtual void OnDirectionChange(OrientationEnum direction)
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

		if (spriteOrder != null)
		{
			if (spriteOrder.Orders.Count > referenceOffset)
			{
				spriteRenderer.sortingOrder = spriteOrder.Orders[referenceOffset];
			}
		}

		//Not networked so don't run sprite change on headless
		if (CustomNetworkManager.IsHeadless) return;

		SpriteHandler.ChangeSpriteVariant(referenceOffset, false);
	}

}
public interface ISpriteOrder{
	SpriteOrder SpriteOrder { get; set; }
}