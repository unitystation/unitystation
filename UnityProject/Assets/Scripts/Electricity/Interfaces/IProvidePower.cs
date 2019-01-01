using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface  IProvidePower  {
	HashSet<IElectricityIO> connectedDevices {get; set;}
	HashSet<IElectricityIO> DirectionWorkOnNextList {get; set;}
	HashSet<IElectricityIO> DirectionWorkOnNextListWait {get; set;}
	HashSet<IElectricityIO> ResistanceWorkOnNextList {get; set;}
	HashSet<IElectricityIO> ResistanceWorkOnNextListWait {get; set;} 
	ElectronicData Data {get; set;}
	IntrinsicElectronicData InData  {get; set;}
}
