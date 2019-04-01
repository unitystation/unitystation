﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ElectronicData  { //to Store data about the electrical device
	
	/// <summary>
	/// Stores for each supply how is the supply connected to that Device.
	/// </summary>
	public Dictionary<IElectricityIO,HashSet<PowerTypeCategory>> ResistanceToConnectedDevices = new Dictionary<IElectricityIO,HashSet<PowerTypeCategory>>(); 

	/// <summary>
	/// The things connected in the vicinity of this
	/// </summary>
	public List<IElectricityIO> connections = new List<IElectricityIO> ();

	public Dictionary<int,Dictionary<IElectricityIO,float>> CurrentGoingTo = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
	public Dictionary<int,Dictionary<IElectricityIO,float>> CurrentComingFrom  = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
	public Dictionary<int, Dictionary<IElectricityIO, float>> ResistanceGoingTo = new Dictionary<int, Dictionary<IElectricityIO, float>>();
	public Dictionary<int,Dictionary<IElectricityIO,float>> ResistanceComingFrom = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
	public Dictionary<int,float> SourceVoltages = new Dictionary<int, float> ();
	public Dictionary<int,HashSet<IElectricityIO>> Downstream = new Dictionary<int, HashSet<IElectricityIO>> ();
	public Dictionary<int,HashSet<IElectricityIO>> Upstream = new Dictionary<int, HashSet<IElectricityIO>> ();
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
