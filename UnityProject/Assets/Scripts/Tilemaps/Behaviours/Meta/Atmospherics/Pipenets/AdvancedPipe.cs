using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//scrubbers, vents, pumps, etc
public class AdvancedPipe : Pipe
{


	public override void Detach()
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			nodes[i].nodes.Remove(this);
		}
		nodes = new List<Pipe>();
	}

}
