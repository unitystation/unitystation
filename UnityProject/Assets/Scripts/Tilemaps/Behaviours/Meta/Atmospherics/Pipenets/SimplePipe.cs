using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class SimplePipe : Pipe
{
	public override void SpriteChange()
	{
		if(objectBehaviour.isNotPushable == false)
		{
			base.SpriteChange();
			return;
		}
		switch (nodes.Count)
		{
			case 2:
				spriteRenderer.sprite = pipeSprites[1];
				break;

			case 1:
				var pipe = nodes[0];
				if(pipe.transform.position.y > transform.position.y)
				{
					spriteRenderer.sprite = pipeSprites[2];
				}
				else
				{
					spriteRenderer.sprite = pipeSprites[3];
				}
				break;

			case 0:
				spriteRenderer.sprite = pipeSprites[4];
				break;

		}
	}


}
