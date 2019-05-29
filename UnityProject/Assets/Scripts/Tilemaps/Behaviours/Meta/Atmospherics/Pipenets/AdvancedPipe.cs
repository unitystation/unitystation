using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//scrubbers, vents, pumps, etc
public class AdvancedPipe : Pipe
{
	private void Start()
	{
		var registerTile = GetComponent<RegisterTile>();
		if (registerTile.isServer)
		{
			UpdateManager.Instance.Add(UpdateMe);
		}
	}

	public virtual void UpdateMe()
	{

	}

	public override void SpriteChange()
	{
		if (objectBehaviour.isNotPushable == false)
		{
			base.SpriteChange();
			return;
		}
		if(nodes.Count == 1)
		{
			spriteRenderer.sprite = pipeSprites[1];
		}
	}

	public override void CalculateDirection()
	{
		direction = 0;
		var rotation = transform.rotation.eulerAngles.z;
		if (rotation >= 45 && rotation <= 135)
		{
			direction = Direction.EAST;
		}
		else
		{
			if (rotation > 135 && rotation < 225)
			{
				direction = Direction.NORTH;
			}
			else
			{
				if (rotation > 225 && rotation < 315)
				{
					direction = Direction.WEST;
				}
				else
				{
					direction = Direction.SOUTH;
				}
			}
		}
	}
}
