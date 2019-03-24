using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface  IProvidePower  {
	HashSet<IElectricityIO> connectedDevices {get; set;}
	ElectronicData Data {get; set;}
	IntrinsicElectronicData InData  {get; set;}
}
