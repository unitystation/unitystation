using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class SimplePipe : Pipe
{
	public SpriteRenderer[] conectionRenderer;
	public Sprite[] connectionSprite;

	public override void CalculateSprite()
	{
		for (int i = 0; i < conectionRenderer.Length; i++)
		{
			conectionRenderer[i].sprite = null;
		}
		if(objectBehaviour.isNotPushable == false)
		{
			base.CalculateSprite();
			return;
		}
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

	public override void CalculateDirection()
	{
		direction = 0;
		var rotation = transform.rotation.eulerAngles.z;
		transform.rotation = Quaternion.identity;
		if ((rotation >= 45 && rotation < 135))
		{
			SetSprite(3);
			DirectionEast();
		}
		else if (rotation >= 135 && rotation < 225)
		{
			SetSprite(2);
			DirectionNorth();
		}
		else if (rotation >= 225 && rotation < 315)
		{
			SetSprite(4);
			DirectionWest();
		}
		else
		{
			SetSprite(1);
			DirectionSouth();
		}
	}

	public virtual void DirectionEast()
	{
		direction = Direction.EAST | Direction.WEST;
	}

	public virtual void DirectionNorth()
	{
		direction = Direction.NORTH | Direction.SOUTH;
	}

	public virtual void DirectionWest()
	{
		direction = Direction.EAST | Direction.WEST;
	}

	public virtual void DirectionSouth()
	{
		direction = Direction.NORTH | Direction.SOUTH;
	}


}
