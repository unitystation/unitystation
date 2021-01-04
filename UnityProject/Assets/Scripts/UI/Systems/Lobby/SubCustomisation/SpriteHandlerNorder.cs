using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteHandlerNorder : MonoBehaviour, ISpriteOrder
{
	public SpriteHandler SpriteHandler;
	public SpriteOrder SpriteOrder { get; set; }

	public SpriteRenderer spriteRenderer;
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

		if (SpriteOrder != null)
		{
			if (SpriteOrder.Orders.Count > referenceOffset)
			{
				spriteRenderer.sortingOrder = SpriteOrder.Orders[referenceOffset];
			}
		}

		SpriteHandler.ChangeSpriteVariant(referenceOffset);
	}

}
public interface ISpriteOrder{
	SpriteOrder SpriteOrder { get; set; }
}