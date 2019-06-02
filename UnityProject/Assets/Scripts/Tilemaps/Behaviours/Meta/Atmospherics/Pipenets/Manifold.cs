using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manifold : SimplePipe
{

	public Sprite[] connectionSprite;
	public SpriteRenderer[] conectionRenderer;

	public override void CalculateSprite()
	{
		for (int i = 0; i < conectionRenderer.Length; i++)
		{
			conectionRenderer[i].sprite = null;
		}
		if (anchored)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				var pipe = nodes[i];
				if (pipe.transform.position.y < transform.position.y)
				{
					conectionRenderer[0].sprite = connectionSprite[0];
				}
				else if (pipe.transform.position.y > transform.position.y)
				{
					conectionRenderer[1].sprite = connectionSprite[1];
				}
				else if (pipe.transform.position.x > transform.position.x)
				{
					conectionRenderer[2].sprite = connectionSprite[2];
				}
				else if (pipe.transform.position.x < transform.position.x)
				{
					conectionRenderer[3].sprite = connectionSprite[3];
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
		SetSprite(3);
		direction = Direction.EAST | Direction.NORTH | Direction.SOUTH;
	}

	public override void DirectionNorth()
	{
		SetSprite(2);
		direction = Direction.WEST | Direction.NORTH | Direction.EAST;
	}

	public override void DirectionWest()
	{
		SetSprite(4);
		direction = Direction.WEST | Direction.SOUTH | Direction.NORTH;
	}

	public override void DirectionSouth()
	{
		SetSprite(1);
		direction = Direction.EAST | Direction.SOUTH | Direction.WEST;
	}

}
