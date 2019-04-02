using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface  IElectricalNeedUpdate {
	void PowerUpdateStructureChange();
	void PowerUpdateStructureChangeReact();
	void InitialPowerUpdateResistance();
	void PowerUpdateResistanceChange();
	void PowerUpdateCurrentChange ();
	void PowerNetworkUpdate ();
	ElectricalOIinheritance _IElectricityIO { get; set; }
	GameObject GameObject();
}