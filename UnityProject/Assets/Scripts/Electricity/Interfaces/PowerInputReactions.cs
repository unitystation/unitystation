using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerInputReactions {
	public PowerTypeCategory ConnectingDevice;
	public bool DirectionReaction = false;
	public bool ResistanceReaction = false; 
	public bool ElectricalReaction  = false;

	public DirectionReactionClass DirectionReactionA = new DirectionReactionClass();
	public ResistanceReactionClass ResistanceReactionA = new ResistanceReactionClass();
	public ElectricalReactionClass ElectricalReactionA = new ElectricalReactionClass();

	public class DirectionReactionClass {
		public BoolClass AddResistanceCall = new BoolClass();
		public bool YouShallNotPass = false;
	}
	public class ResistanceReactionClass {
		public FloatClass Resistance = new FloatClass();
		public bool YouShallNotPass = false;
	}
	public class ElectricalReactionClass {
		public float Current; //I don't really know 
		public bool YouShallNotPass = false;
	}
}
