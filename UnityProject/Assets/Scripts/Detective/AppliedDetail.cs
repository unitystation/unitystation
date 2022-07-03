using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppliedDetail
{
	public HashSet<int> Interacted = new HashSet<int>();

	public List<Detail> Details = new List<Detail>();

	public System.Random RNG = new System.Random();

	public void AddDetail(Detail Detail)
	{
		if (Interacted.Contains(Detail.CausedByInstanceID)) return;
		if (Details.Count == 15)
		{
			Details.RemoveAt(RNG.Next(0, Details.Count-1));
		}

		if (Details.Count > 0)
		{
			Details.Insert(RNG.Next(0, Details.Count-1), Detail);
		}
		else
		{
			Details.Add(Detail);
		}

		Interacted.Add(Detail.CausedByInstanceID);
	}

	public void Clean()
	{
		Interacted.Clear();
		Details.Clear();
	}

}




public class Detail
{
	public int CausedByInstanceID;
	public string Description;
	public DetailType DetailType;

}

public enum DetailType
{
	Fibre, //Any interaction????
	Fingerprints, //Any interaction???? humm,  tries to get clear script of Performer?
	SpeciesIdentify,

	Footprints, //called by Footstep code Expensive though humm
	Blood, //idk Need blood Splats

	BulletHole, //Applied Decal call
}

//

//fibre
//CausedByInstanceID,
//Description



//footprints
//CausedByInstanceID
//Description


//fingerprints
//CausedByInstanceID
//Description

//blood
//Description
//CausedByInstanceID ??


//Bullet hole
//Description
//CausedByInstanceID

//Bullet hole
//Description (damage)
//CausedByInstanceID
