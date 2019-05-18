using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePipe : Pipe
{
	public Pipenet pipenet;
	public float volume = 70;

	public int ARANpipenetmembers;
	public string ARANpipenetName;
	public float ARANpipenetVolume;

	private void Update() {
		if(pipenet != null){
			ARANpipenetmembers = pipenet.members.Count;
			ARANpipenetName = pipenet.ARANname;
			ARANpipenetVolume = pipenet.gasMix.Volume;
		}
		else
		{
			ARANpipenetVolume = 0;
			ARANpipenetmembers = 0;
			ARANpipenetName = "NONE";
		}
	}

	public override void Attach(){
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



	public override void Detach(){
		//TODO: release gas to environmental air

		int neighboorSimplePipes = 0;
		for (int i = 0; i < nodes.Count; i++)
		{
			var pipe = nodes[i];
			pipe.nodes.Remove(this);
			var simplePipe = pipe.GetComponent<SimplePipe>();
			if(simplePipe != null)
			{
				neighboorSimplePipes ++;
			}
		}
		nodes = new List<Pipe>();

		Pipenet oldPipenet = pipenet;
		pipenet.RemoveSimplePipe(this);

		if (oldPipenet.members.Count == 0)
		{
			//we're the only pipe on the net, delete it
			oldPipenet.DeletePipenet();
			return;
		}

		if(neighboorSimplePipes == 1)
		{
			//we're at an edge of the pipenet, safe to remove
			return;
		}
		oldPipenet.Separate();
	}


}
