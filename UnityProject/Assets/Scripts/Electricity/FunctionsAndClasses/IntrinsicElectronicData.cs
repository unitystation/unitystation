using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntrinsicElectronicData  {
	public PowerTypeCategory Categorytype { get; set; }
	public HashSet<PowerTypeCategory> CanConnectTo {get; set;}
	/// <summary>
	/// if the incoming input is from a certain type of  Machine/cable React differently
	/// </summary>
	public Dictionary<PowerTypeCategory,PowerInputReactions> ConnectionReaction {get; set;} = new Dictionary<PowerTypeCategory,PowerInputReactions>();
	public IDeviceControl ControllingDevice; 
	public IElectricalNeedUpdate ControllingUpdate;
	public bool DirectionOverride = false;
	public bool ResistanceOverride = false;
	public bool ElectricityOverride = false;
}
