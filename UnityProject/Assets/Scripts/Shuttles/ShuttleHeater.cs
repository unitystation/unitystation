using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class ShuttleHeater : AdvancedPipe
{
	public override void SpriteChange()
	{
		return;
	}

	public override void SetSpriteLayer()
	{
		return;
	}

	public override void SetAnchored(bool value)
	{
		anchored = value;
	}

	public override void CalculateDirection()
	{
		direction = 0;
		var rotation = transform.rotation.eulerAngles.z;
		if (rotation >= 45 && rotation <= 135)
		{
			direction = Direction.WEST;
		}
		else
		{
			if (rotation > 135 && rotation < 225)
			{
				direction = Direction.SOUTH;
			}
			else
			{
				if (rotation > 225 && rotation < 315)
				{
					direction = Direction.EAST;
				}
				else
				{
					direction = Direction.NORTH;
				}
			}
		}
	}
}
