using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class SimplePipe : Pipe
{
	public Sprite[] connectionSprite;
	public SpriteRenderer[] conectionRenderer;

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

	public override void CalculateSprite()
	{
		for (int i = 0; i < conectionRenderer.Length; i++)
		{
			conectionRenderer[i].sprite = null;
		}
		base.CalculateSprite();
		if(anchored)
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
	}

}
