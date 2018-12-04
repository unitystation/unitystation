using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerInputReactions
{ //To allow for different resistance depending on which connection method you are using
	public PowerTypeCategory ConnectingDevice;
	public bool DirectionReaction = false;
	public bool ResistanceReaction = false;
	public bool ElectricalReaction = false;

	public DirectionReactionClass DirectionReactionA = new DirectionReactionClass();
	public ResistanceReactionClass ResistanceReactionA = new ResistanceReactionClass();
	public ElectricalReactionClass ElectricalReactionA = new ElectricalReactionClass();

	public class DirectionReactionClass
	{
		public Resistance AddResistanceCall = new Resistance(); //For checking if Resistance is available (ResistanceAvailable)
		public bool YouShallNotPass = false; //can use the device as pass through
	}
	public class ResistanceReactionClass
	{
		public Resistance Resistance = new Resistance(); //Specifies how much resistance it should show
		public bool YouShallNotPass = false;
	}
	public class ElectricalReactionClass
	{ //Doesn't do anything current
		public float Current; //I don't really know 
		public bool YouShallNotPass = false;
	}
}