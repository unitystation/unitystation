using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class SimplePipe : Pipe
{
	public override void CalculateSprite()
	{
		if(objectBehaviour.isNotPushable == false)
		{
			base.CalculateSprite();
			return;
		}
		switch (nodes.Count)
		{
			case 2:
				SetSprite(1);
				break;

			case 1:
				var pipe = nodes[0];
				if (HasDirection(direction, Direction.NORTH))
				{
					if(pipe.transform.position.y > transform.position.y)
					{
						SetSprite(2);
					}
					else
					{
						SetSprite(3);
					}
				}
				else
				{
					if (pipe.transform.position.x < transform.position.x)
					{
						SetSprite(2);
					}
					else
					{
						SetSprite(3);
					}
				}
				break;

			case 0:
				SetSprite(4);
				break;

		}
	}


}
