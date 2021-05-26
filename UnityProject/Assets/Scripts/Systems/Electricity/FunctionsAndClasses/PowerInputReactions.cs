using System;
using System.Collections;
using System.Collections.Generic;

namespace Systems.Electricity
{
	/// <summary>
	/// To allow for different resistance depending on which connection method you are using
	/// </summary>
	[Serializable]
	public class PowerInputReactions
	{
		public PowerTypeCategory ConnectingDevice;
		public bool DirectionReaction = false;
		public bool ResistanceReaction = false;

		public DirectionReactionClass DirectionReactionA = new DirectionReactionClass();
		public ResistanceReactionClass ResistanceReactionA = new ResistanceReactionClass();

		[Serializable]
		public class DirectionReactionClass
		{
			public bool YouShallNotPass = false; // can use the device as pass through
		}

		[Serializable]
		public class ResistanceReactionClass
		{
			public Resistance Resistance = new Resistance(); // Specifies how much resistance it should show
		}
	}
}
