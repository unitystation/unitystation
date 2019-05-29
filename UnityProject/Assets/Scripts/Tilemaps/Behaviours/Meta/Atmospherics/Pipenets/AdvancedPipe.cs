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
}
