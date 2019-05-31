using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FourWayManifold : SimplePipe
{
	public override void DirectionEast()
	{
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

	public override void DirectionNorth()
	{
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

	public override void DirectionWest()
	{
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

	public override void DirectionSouth()
	{
		direction = Direction.NORTH | Direction.SOUTH | Direction.WEST | Direction.EAST;
	}

}
