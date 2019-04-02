using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInLineDevices {

	float ModifyElectricityInput(float Current, GameObject SourceInstance,  ElectricalOIinheritance ComingFrom);
	float ModifyElectricityOutput(float Current, GameObject SourceInstance);

	float ModifyResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom  );
	float ModifyResistancyOutput(float Resistance, GameObject SourceInstance);

	//void ModifyDirectionInput(int tick, GameObject SourceInstance, ElectricalOIinheritance ComingFrom);
	//void ModifyDirectionOutput (int tick, GameObject SourceInstanc);
}
