using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FourWayManifold : Manifold
{
	public override void DirectionEast()
	{
		SetSprite(1);
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

	public override void DirectionNorth()
	{
		SetSprite(1);
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

	public override void DirectionWest()
	{
		SetSprite(1);
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

	public override void DirectionSouth()
	{
		SetSprite(1);
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

}
