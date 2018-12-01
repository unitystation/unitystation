using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerInputReactions { //To allow for different resistance depending on which connection method you are using
	public PowerTypeCategory ConnectingDevice;
	public bool DirectionReaction = false;
	public bool ResistanceReaction = false; 
	public bool ElectricalReaction  = false;

	public DirectionReactionClass DirectionReactionA = new DirectionReactionClass();
	public ResistanceReactionClass ResistanceReactionA = new ResistanceReactionClass();
	public ElectricalReactionClass ElectricalReactionA = new ElectricalReactionClass();

	public class DirectionReactionClass {
		public BoolClass AddResistanceCall = new BoolClass(); //Says the supply I can provide a resistance
		public bool YouShallNotPass = false; //can use the device as pass through
	}
	public class ResistanceReactionClass {
		public FloatClass Resistance = new FloatClass(); //Specifies how much resistance it should show
		public bool YouShallNotPass = false;
	}
	public class ElectricalReactionClass { //Doesn't do anything current
		public float Current; //I don't really know 
		public bool YouShallNotPass = false;
	}
}
