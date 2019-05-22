using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class Pipenet
{
	public GasMix gasMix;
	public List<Pipe> members = new List<Pipe>();

	public Pipenet() {
		gasMix = new GasMix(GasMixes.Empty);
	}

	public void AddPipe(Pipe pipe){
		for (int i = 0; i < pipe.nodes.Count; i++)
		{
			var attachedPipe = pipe.nodes[i];
			if(attachedPipe.pipenet != this){
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
		AddPipeEXTRA(pipe);
	}

	public void MergePipenet(Pipenet otherPipenet){
		for (int i = 0; i < otherPipenet.members.Count; i++)
		{
			var pipe = otherPipenet.members[i];
			AddPipeEXTRA(pipe);
		}
		otherPipenet.members = new List<Pipe>();
		gasMix += otherPipenet.gasMix;
		otherPipenet.DeletePipenet();
	}

	public void DeletePipenet()
	{
		for (int i = 0; i < members.Count; i++)
		{
			members[i].pipenet = null;
		}
	}

	public void AddPipeEXTRA(Pipe pipe)
	{
		members.Add(pipe);
		pipe.pipenet = this;
		gasMix.ChangeVolumeValue(pipe.volume);
	}

	public void RemovePipe(Pipe pipe)
	{
		pipe.pipenet = null;
		members.Remove(pipe);
		gasMix.ChangeVolumeValue( - pipe.volume);
	}

	public void Separate()
	{
		var oldGasMix = gasMix;
		var oldMembers = members;
		gasMix = new GasMix(GasMixes.Empty);
		members = new List<Pipe>();

		for (int i = 0; i < oldMembers.Count; i++)
		{
			oldMembers[i].pipenet = null;
		}

		Pipenet newPipenet = this;
		var separatedPipenets = new List<Pipenet>(){this};
		for (int i = 0; i < oldMembers.Count; i++)
		{
			var pipe = oldMembers[i];
			if(pipe.pipenet == null)
			{
				if(newPipenet == null)
				{
					newPipenet = new Pipenet();
					separatedPipenets.Add(newPipenet);
				}
				newPipenet.SpreadPipenet(pipe);
				newPipenet = null;
			}
		}

		oldGasMix = oldGasMix/oldGasMix.Volume;
		for (int i = 0; i < separatedPipenets.Count; i++)
		{
			var pipenet = separatedPipenets[i];
			var oldVolume = pipenet.gasMix.Volume;
			pipenet.gasMix = oldGasMix * pipenet.gasMix.Volume;
			pipenet.gasMix.ChangeVolumeValue( - (pipenet.gasMix.Volume - oldVolume));
		}

	}

	public void SpreadPipenet(Pipe pipe)
	{

		List<Pipe> foundPipes = new List<Pipe>();
		foundPipes.Add(pipe);
		while(foundPipes.Count > 0)
		{
			var foundPipe = foundPipes[0];
			AddPipeEXTRA(foundPipe);
			foundPipes.Remove(foundPipe);
			for (int i = 0; i < foundPipe.nodes.Count; i++)
			{
				var nextPipe = foundPipe.nodes[i];
				if(nextPipe.pipenet == null)
				{
					foundPipes.Add(nextPipe);
				}
			}
		}
	}
}
