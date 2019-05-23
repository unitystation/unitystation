using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//scrubbers, vents, pumps, etc
public class AdvancedPipe : Pipe
{

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
}
