using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//scrubbers, vents, pumps, etc
public class AdvancedPipe : Pipe
{


	public void AttachPipe(Pipe pipe)
	{

	}

	public void DetachPipe(Pipe pipe)
	{
		nodes.Remove(pipe);
		var simplePipe = pipe.GetComponent<SimplePipe>();
		if(simplePipe != null)
		{
			DetachSimplePipe(simplePipe);
		}
	}


	public void DetachSimplePipe(SimplePipe detachingPipe)
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			var simplePipe = nodes[i].GetComponent<SimplePipe>();
			if(simplePipe != null)
			{
				if(detachingPipe.pipenet == simplePipe.pipenet)
				{
					//we're still connected to the same pipenet through another port, do nothing
					return;
				}
			}
		}

		detachingPipe.pipenet.advancedPipes.Remove(this);
	}

}
