using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface  IIElectricalOIinheritance  {
	HashSet<ElectricalOIinheritance> connectedDevices {get; set;}
	ElectronicData Data {get; set;}
	IntrinsicElectronicData InData  {get; set;}
}
