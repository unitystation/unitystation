using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class ShuttleHeater : AdvancedPipe
{
	public override void CalculateSprite()
	{
		return;
	}

	public override void SetSpriteLayer(bool anchoredLayer)
	{
		return;
	}

	public override void SetAnchored(bool value)
	{
		anchored = value;
		pickupable.ServerSetCanPickup(!value);
	}

	public override void DirectionEast()		//shuttle sprites are upside down, turn direction
	{
		direction = Direction.WEST;
	}

	public override void DirectionNorth()
	{
		direction = Direction.SOUTH;
	}

	public override void DirectionWest()
	{
		direction = Direction.WEST;
	}

	public override void DirectionSouth()
	{
		direction = Direction.SOUTH;
	}

}