using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ElectronicData  { //to Store data about the electrical device
	
	/// <summary>
	/// Stores for each supply how is the supply connected to that Device.
	/// </summary>
	public Dictionary<ElectricalOIinheritance,Dictionary<ElectricalOIinheritance, ElectronicStepAndProcessed>> 
	ResistanceToConnectedDevices = new Dictionary<ElectricalOIinheritance, Dictionary<ElectricalOIinheritance, ElectronicStepAndProcessed>>();

	public Dictionary<ElectricalOIinheritance, ElectronicSupplyData> SupplyDependent = new Dictionary<ElectricalOIinheritance, ElectronicSupplyData>();
	/// <summary>
	/// The things connected in the vicinity of this
	/// </summary>
	public HashSet<ElectricalOIinheritance> connections = new HashSet<ElectricalOIinheritance> ();
	public float CurrentInWire;
	public float ActualVoltage;
	public float EstimatedResistance;
	public float SupplyingCurrent; //Doesn't really need to access this from the data but no harm in keeping it around  could access from the  supplying device module
	public float InternalResistance;
	public float SupplyingVoltage;
	public bool ChangeToOff;

	public float CurrentStoreValue; //I'm lazy and it's cheaper than making a key value And putting it into a hash set
}

public class ElectronicStepAndProcessed {
	public HashSet<ElectricalDirectionStep> Steps = new HashSet<ElectricalDirectionStep>();
	public bool BeenProcessed = false; 
}
