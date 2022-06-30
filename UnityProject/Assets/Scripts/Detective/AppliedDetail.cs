using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppliedDetail
{
	public List<Detail> Details = new List<Detail>();


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

	Footprints, //called by Footstep code
	Blood, //idk Need blood Splats
	BulletHole, //Applied Decal call
	Damage //Applied by damage function
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
