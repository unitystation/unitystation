using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntrinsicElectronicData  {
	public PowerTypeCategory Categorytype { get; set; }
	public HashSet<PowerTypeCategory> CanConnectTo {get; set;}
	public Dictionary<PowerTypeCategory,PowerInputReactions> ConnectionReaction {get; set;} = new Dictionary<PowerTypeCategory,PowerInputReactions>();
	public IDeviceControl ControllingDevice; 
	public IElectricalNeedUpdate ControllingUpdate; 
}
