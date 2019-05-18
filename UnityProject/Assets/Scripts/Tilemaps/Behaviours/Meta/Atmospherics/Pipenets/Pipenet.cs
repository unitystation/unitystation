using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipenet
{
	public Atmospherics.GasMix gasMix;
	public List<SimplePipe> members = new List<SimplePipe>();

	public Pipenet() {
		gasMix = new Atmospherics.GasMix(Atmospherics.GasMixes.Empty);
	}

	public void AddPipe(SimplePipe simplePipe){
		for (int i = 0; i < simplePipe.nodes.Count; i++)
		{
			var attachedPipe = simplePipe.nodes[i].GetComponent<SimplePipe>();
			if(attachedPipe && attachedPipe.pipenet != this){
				if(this.members.Count >= attachedPipe.pipenet.members.Count)
				{
					MergePipenet(attachedPipe.pipenet);
				}
				else
				{
					attachedPipe.pipenet.MergePipenet(this);
					attachedPipe.pipenet.AddPipe(simplePipe);
					return;
				}
			}
		}
		AddSimplePipe(simplePipe);
	}

	public void MergePipenet(Pipenet otherPipenet){
		for (int i = 0; i < otherPipenet.members.Count; i++)
		{
			SimplePipe simplePipe = otherPipenet.members[i];
			AddSimplePipe(simplePipe);
		}
		otherPipenet.members = new List<SimplePipe>();
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

	public void AddSimplePipe(SimplePipe simplePipe)
	{
		members.Add(simplePipe);
		simplePipe.pipenet = this;
		gasMix.ChangeVolumeValue(simplePipe.volume);
	}

	public void RemoveSimplePipe(SimplePipe simplePipe)
	{
		simplePipe.pipenet = null;
		members.Remove(simplePipe);
		gasMix.ChangeVolumeValue( - simplePipe.volume);
	}

	public void Separate()
	{
		var oldGasMix = gasMix;
		var oldMembers = members;
		gasMix = new Atmospherics.GasMix(Atmospherics.GasMixes.Empty);
		members = new List<SimplePipe>();

		for (int i = 0; i < oldMembers.Count; i++)
		{
			oldMembers[i].pipenet = null;
		}

		Pipenet newPipenet = this;
		var separatedPipenets = new List<Pipenet>(){this};
		for (int i = 0; i < oldMembers.Count; i++)
		{
			var simplePipe = oldMembers[i];
			if(simplePipe.pipenet == null)
			{
				if(newPipenet == null)
				{
					newPipenet = new Pipenet();
					separatedPipenets.Add(newPipenet);
					newPipenet.ARANname = Random.Range(0,100).ToString();
				}
				newPipenet.SpreadPipenet(simplePipe);
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

	public string ARANname = "FIRST";
	public void SpreadPipenet(SimplePipe startPipe)
	{

		List<SimplePipe> foundPipes = new List<SimplePipe>();
		foundPipes.Add(startPipe);
		while(foundPipes.Count > 0)
		{
			var simplePipe = foundPipes[0];
			AddSimplePipe(simplePipe);
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
			}
		}
	}
}
