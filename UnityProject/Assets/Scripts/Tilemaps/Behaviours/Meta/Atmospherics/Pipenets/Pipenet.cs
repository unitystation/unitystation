using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipenet
{
	public Atmospherics.GasMix gasMix;
	public List<SimplePipe> members = new List<SimplePipe>();

	public List<Atmospherics.GasMix> otherAirs = new List<Atmospherics.GasMix>();	//are these two lists useless?
	public List<AdvancedPipe> advancedPipes = new List<AdvancedPipe>();


	public void AddPipe(SimplePipe pipe){
		for (int i = 0; i < pipe.nodes.Count; i++)
		{
			var attachedPipe = pipe.nodes[i].GetComponent<SimplePipe>();
			if(attachedPipe && attachedPipe.pipenet != this){
				if(this.members.Count >= attachedPipe.pipenet.members.Count)
				{
					MergePipenet(attachedPipe.pipenet);
				}
				else
				{
					attachedPipe.pipenet.MergePipenet(this);
					attachedPipe.pipenet.AddPipe(pipe);
					return;
				}
			}
		}

		members.Add(pipe);
		pipe.pipenet = this;
		//add the pipe.volume to the gasmix
	}

	public void MergePipenet(Pipenet otherPipenet){
		for (int i = 0; i < otherPipenet.members.Count; i++)
		{
			SimplePipe pipe = otherPipenet.members[i];
			members.Add(pipe);
			pipe.pipenet = this;
			//add the pipe.volume to the gasmix
		}
		//merge airs here, including volume
		otherPipenet.DeletePipenet();
	}

	public void DeletePipenet()
	{
		for (int i = 0; i < members.Count; i++)
		{
			members[i].pipenet = null;
		}
		members.Clear(); //necessary?
		advancedPipes.Clear(); //necessary?
	}

	public void RemoveSimplePipe(SimplePipe simplePipe)
	{
		members.Remove(simplePipe);
	}

	public void Separate()
	{
		advancedPipes.Clear();
		for (int i = 0; i < members.Count; i++)
		{
			members[i].pipenet = null;
		}

		Pipenet newPipenet = this;
		for (int i = 0; i < members.Count; i++)
		{
			if(members[i].pipenet == null)
			{
				if(newPipenet == null)
				{
					newPipenet = new Pipenet();
				}
				newPipenet.SpreadPipenet(members[i]);
				newPipenet = null;
			}

		}
	}

	public void SpreadPipenet(SimplePipe startPipe)
	{
		List<SimplePipe> foundPipes = new List<SimplePipe>();
		foundPipes.Add(startPipe);
		while(foundPipes.Count > 0)
		{
			var simplePipe = foundPipes[0];
			simplePipe.pipenet = this;
			foundPipes.Remove(simplePipe);
			for (int i = 0; i < simplePipe.nodes.Count; i++)
			{
				var nextPipe = simplePipe.nodes[i].GetComponent<SimplePipe>();
				if(nextPipe != null)
				{
					if(nextPipe.pipenet == null)
					{
						foundPipes.Add(nextPipe);
					}
				}
				else
				{
					var advancedPipe = simplePipe.nodes[i].GetComponent<AdvancedPipe>();
					advancedPipes.Add(advancedPipe);
				}
			}
		}
	}
}
