using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInLineDevices {

	float ModifyElectricityInput(float Current, GameObject SourceInstance,  IElectricityIO ComingFrom);
	float ModifyElectricityOutput(float Current, GameObject SourceInstance);

	float ModifyResistanceInput(float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  );
	float ModifyResistancyOutput(float Resistance, GameObject SourceInstance);

	//void ModifyDirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom);
	//void ModifyDirectionOutput (int tick, GameObject SourceInstanc);
}
