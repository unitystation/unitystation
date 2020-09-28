using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class PowerInputReactions
{ //To allow for different resistance depending on which connection method you are using
	public PowerTypeCategory ConnectingDevice;
	public bool DirectionReaction = false;
	public bool ResistanceReaction = false;

	public DirectionReactionClass DirectionReactionA = new DirectionReactionClass();
	public ResistanceReactionClass ResistanceReactionA = new ResistanceReactionClass();
	[System.Serializable]
	public class DirectionReactionClass
	{
		public bool YouShallNotPass = false; //can use the device as pass through
	}
	[System.Serializable]
	public class ResistanceReactionClass
	{
		public Resistance Resistance = new Resistance(); //Specifies how much resistance it should show
	}

}