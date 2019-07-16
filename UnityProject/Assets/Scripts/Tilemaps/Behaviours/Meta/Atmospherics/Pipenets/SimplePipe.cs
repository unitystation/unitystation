using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class SimplePipe : Pipe
{

	public override void CalculateSprite()
	{
		if (anchored)
		{
			if(nodes.Count == 2)
			{
				if(HasDirection(direction, Direction.SOUTH))
				{
					SetSprite(1);
				}
				else
				{
					SetSprite(2);
				}
			}
			else if(nodes.Count == 1)
			{
				for (int i = 0; i < nodes.Count; i++)
				{
					var pipe = nodes[i];
					if (pipe.registerTile.WorldPositionServer.y < registerTile.WorldPositionServer.y)
					{
						SetSprite(3);
					}
					else if (pipe.registerTile.WorldPositionServer.y > registerTile.WorldPositionServer.y)
					{
						SetSprite(4);
					}
					else if (pipe.registerTile.WorldPositionServer.x > registerTile.WorldPositionServer.x)
					{
						SetSprite(5);
					}
					else if (pipe.registerTile.WorldPositionServer.x < registerTile.WorldPositionServer.x)
					{
						SetSprite(6);
					}
				}
			}
			else if(nodes.Count == 0)
			{
				if (HasDirection(direction, Direction.SOUTH))
				{
					SetSprite(7);
				}
				else
				{
					SetSprite(8);
				}
			}
		}
		else
		{
			SetSprite(0);
		}
	}


	public override void DirectionEast()
	{
		direction = Direction.EAST | Direction.WEST;
	}

	public override void DirectionNorth()
	{
		direction = Direction.NORTH | Direction.SOUTH;
	}

	public override void DirectionWest()
	{
		direction = Direction.EAST | Direction.WEST;
	}

	public override void DirectionSouth()
	{
		direction = Direction.NORTH | Direction.SOUTH;
	}

}
