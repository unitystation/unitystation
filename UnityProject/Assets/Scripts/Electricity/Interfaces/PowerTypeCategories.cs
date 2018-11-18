using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PowerTypeCategory {
	APC,
	StandardCable,
	SMES,
	FieldGenerator,
	MediumMachineConnector,
	RadiationCollector,
	Transformer,
	DepartmentBattery,
	LowVoltageCable,
	LowMachineConnector,
	HighMachineConnector,
	HighVoltageCable,
	DeadEndConnection,
}//hey Be careful when changing this because it's stored as numbers in prefabs/saved scenes for some stupid reason