using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePipe : Pipe
{
	public Pipenet pipenet;
	public float volume;


	public override void Attach(){
		CalculateAttachedNodes();

		Pipenet foundPipenet = null;
		for (int i = 0; i < nodes.Count; i++)
		{
			var simplePipe = nodes[i].GetComponent<SimplePipe>();
			if(simplePipe != null)
			{
				foundPipenet = simplePipe.pipenet;
				break;
			}
		}
		if(foundPipenet == null)
		{
			foundPipenet = new Pipenet();
		}
		foundPipenet.AddPipe(this);
	}

	public void CalculateAttachedNodes(){
		List<Pipe> attachedNodes = new List<Pipe>();
		//TODO: check turfs based on the aim of the pipe and get the pipes and add them to this list
		nodes = attachedNodes;
	}

	public override void Detach(){
		//TODO: release gas to environmental air

		int neighboorSimplePipes = 0;
		for (int i = 0; i < nodes.Count; i++)
		{
			var pipe = nodes[i];
			var advancedPipe = pipe.GetComponent<AdvancedPipe>();
			if(advancedPipe != null)
			{
				advancedPipe.DetachPipe(this);
			}
			else
			{
				neighboorSimplePipes ++;
			}
		}

		pipenet.RemoveSimplePipe(this);

		if (pipenet.members.Count == 0)
		{
			//we're the only pipe on the net, delete it
			pipenet.DeletePipenet();
			return;
		}

		if(neighboorSimplePipes == 1)
		{
			//we're at an edge of the pipenet, safe to remove
			return;
		}

		pipenet.Separate();
	}


}
