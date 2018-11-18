using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInLineDevices {

	float ModifyElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom);
	float ModifyElectricityOutput(int tick, float Current, GameObject SourceInstance);

	float ModifyResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  );
	float ModifyResistancyOutput(int tick, float Resistance, GameObject SourceInstance);

	//void ModifyDirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom);
	//void ModifyDirectionOutput (int tick, GameObject SourceInstanc);
}
