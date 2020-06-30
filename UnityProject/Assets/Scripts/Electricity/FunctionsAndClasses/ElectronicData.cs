using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ElectronicData  { //to Store data about the electrical device

	/// <summary>
	/// Stores for each supply how is the supply connected to that Device.
	/// Supply > Resistance > connections
	/// </summary>
	public Dictionary<ElectricalOIinheritance,Dictionary<Resistance, HashSet<IntrinsicElectronicData>>>
	ResistanceToConnectedDevices = new Dictionary<ElectricalOIinheritance, Dictionary<Resistance, HashSet<IntrinsicElectronicData>>>();

	public Dictionary<ElectricalOIinheritance, ElectronicSupplyData> SupplyDependent = new Dictionary<ElectricalOIinheritance, ElectronicSupplyData>();
	/// <summary>
	/// The things connected in the vicinity of this
	/// </summary>
	public HashSet<IntrinsicElectronicData> connections = new HashSet<IntrinsicElectronicData>();
	public float CurrentInWire;
	public float ActualVoltage;
	public float EstimatedResistance;
	public float SupplyingCurrent; //Doesn't really need to access this from the data but no harm in keeping it around  could access from the  supplying device module
	public float InternalResistance;
	public float SupplyingVoltage;
	public float ProducingWatts;
	public bool ChangeToOff;

	public float CurrentStoreValue; //I'm lazy and it's cheaper than making a key value And putting it into a hash set
}
