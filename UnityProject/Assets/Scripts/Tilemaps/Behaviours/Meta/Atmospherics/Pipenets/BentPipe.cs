using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BentPipe : SimplePipe
{

	public override void CalculateSprite()
	{
		if (anchored)
		{
			if (nodes.Count == 2)
			{
				if (HasDirection(direction, Direction.SOUTH))
				{
					if (HasDirection(direction, Direction.EAST))
					{
						SetSprite(1);
					}
					else
					{
						SetSprite(2);
					}
				}
				else
				{
					if (HasDirection(direction, Direction.EAST))
					{
						SetSprite(3);
					}
					else
					{
						SetSprite(4);
					}
				}
			}
			else if (nodes.Count == 1)
			{
				for (int i = 0; i < nodes.Count; i++)
				{
					var pipe = nodes[i];
					if (pipe.registerTile.WorldPositionServer.y < registerTile.WorldPositionServer.y)
					{
						if (HasDirection(direction, Direction.EAST))
						{
							SetSprite(5);
						}
						else
						{
							SetSprite(6);
						}
					}
					else if (pipe.registerTile.WorldPositionServer.y > registerTile.WorldPositionServer.y)
					{
						if (HasDirection(direction, Direction.EAST))
						{
							SetSprite(7);
						}
						else
						{
							SetSprite(8);
						}
					}
					else if (pipe.registerTile.WorldPositionServer.x > registerTile.WorldPositionServer.x)
					{
						if (HasDirection(direction, Direction.SOUTH))
						{
							SetSprite(9);
						}
						else
						{
							SetSprite(10);
						}
					}
					else if (pipe.registerTile.WorldPositionServer.x < registerTile.WorldPositionServer.x)
					{
						if (HasDirection(direction, Direction.SOUTH))
						{
							SetSprite(11);
						}
						else
						{
							SetSprite(12);
						}
					}
				}
			}
			else if (nodes.Count == 0)
			{
				if (HasDirection(direction, Direction.SOUTH))
				{
					if (HasDirection(direction, Direction.EAST))
					{
						SetSprite(13);
					}
					else
					{
						SetSprite(14);
					}
				}
				else
				{
					if (HasDirection(direction, Direction.EAST))
					{
						SetSprite(15);
					}
					else
					{
						SetSprite(16);
					}
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
		direction = Direction.EAST | Direction.NORTH;
	}

	public override void DirectionNorth()
	{
		direction = Direction.WEST | Direction.NORTH;
	}

	public override void DirectionWest()
	{
		direction = Direction.WEST | Direction.SOUTH;
	}

	public override void DirectionSouth()
	{
		direction = Direction.EAST | Direction.SOUTH;
	}

}
