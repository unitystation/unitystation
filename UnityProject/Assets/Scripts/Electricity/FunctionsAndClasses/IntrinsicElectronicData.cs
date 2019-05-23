using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class IntrinsicElectronicData  {
	public PowerTypeCategory Categorytype;
	public HashSet<PowerTypeCategory> CanConnectTo =new HashSet<PowerTypeCategory>(); 
	/// <summary>
	/// if the incoming input is from a certain type of  Machine/cable React differently
	/// </summary>
	public Dictionary<PowerTypeCategory,PowerInputReactions> ConnectionReaction = new Dictionary<PowerTypeCategory,PowerInputReactions>();
	public ElectricalNodeControl ControllingDevice; 
	public bool DirectionOverride = false;
	public bool ResistanceOverride = false;
	public bool ElectricityOverride = false;
}
