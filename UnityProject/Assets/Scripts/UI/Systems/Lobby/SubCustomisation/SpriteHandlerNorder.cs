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
		if (InNew == null) return;
		Data = InNew;
		if (CustomNetworkManager.Instance._isServer) return;
		spriteOrder = JsonConvert.DeserializeObject<SpriteOrder>(Data);
		SpriteOrder.Orders.RemoveRange(0, 4);
	}

	public void ChangeSpriteVariant(int number)
	{
		SpriteHandler.ChangeSpriteVariant(number);
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

		if (spriteOrder != null)
		{
			if (spriteOrder.Orders.Count > referenceOffset)
			{
				spriteRenderer.sortingOrder = spriteOrder.Orders[referenceOffset];
			}
		}

		SpriteHandler.ChangeSpriteVariant(referenceOffset, false);
	}

}
public interface ISpriteOrder{
	SpriteOrder SpriteOrder { get; set; }
}