using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ElectronicData  { //to Store data about the electrical device
	
	/// <summary>
	/// Stores for each supply how is the supply connected to that Device.
	/// </summary>
	public Dictionary<ElectricalOIinheritance,HashSet<PowerTypeCategory>> ResistanceToConnectedDevices = new Dictionary<ElectricalOIinheritance,HashSet<PowerTypeCategory>>(); 

	/// <summary>
	/// The things connected in the vicinity of this
	/// </summary>
	public List<ElectricalOIinheritance> connections = new List<ElectricalOIinheritance> ();
	public Dictionary<int,Dictionary<ElectricalOIinheritance,float>> CurrentGoingTo = new Dictionary<int, Dictionary<ElectricalOIinheritance, float>> ();
	public Dictionary<int,Dictionary<ElectricalOIinheritance,float>> CurrentComingFrom  = new Dictionary<int, Dictionary<ElectricalOIinheritance, float>> ();
	public Dictionary<int, Dictionary<ElectricalOIinheritance, float>> ResistanceGoingTo = new Dictionary<int, Dictionary<ElectricalOIinheritance, float>>();
	public Dictionary<int,Dictionary<ElectricalOIinheritance,float>> ResistanceComingFrom = new Dictionary<int, Dictionary<ElectricalOIinheritance, float>> ();
	public Dictionary<int,float> SourceVoltages = new Dictionary<int, float> ();
	public Dictionary<int,HashSet<ElectricalOIinheritance>> Downstream = new Dictionary<int, HashSet<ElectricalOIinheritance>> ();
	public Dictionary<int,HashSet<ElectricalOIinheritance>> Upstream = new Dictionary<int, HashSet<ElectricalOIinheritance>> ();
	public int FirstPresent;
	public Electricity ActualCurrentChargeInWire = new Electricity();
	public float UpstreamCount;
	public float DownstreamCount;
	public float CurrentInWire;
	public float ActualVoltage;
	public float EstimatedResistance;
	public float SupplyingCurrent;
	public float InternalResistance;
	public float SupplyingVoltage;
	public bool ChangeToOff;

	public float CurrentStoreValue; //I'm lazy and it's cheaper than making a key value And putting it into a hash set
}

