using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manifold : SimplePipe
{
	public override void DirectionEast()//3
	{
		direction = Direction.EAST | Direction.NORTH | Direction.SOUTH;
	}

	public override void DirectionNorth()// 2
	{
		direction = Direction.WEST | Direction.NORTH | Direction.EAST;
	}

	public override void DirectionWest()// 4
	{
		direction = Direction.WEST | Direction.SOUTH | Direction.NORTH;
	}

	public override void DirectionSouth() //1
	{
		direction = Direction.EAST | Direction.SOUTH | Direction.WEST;
	}

}
