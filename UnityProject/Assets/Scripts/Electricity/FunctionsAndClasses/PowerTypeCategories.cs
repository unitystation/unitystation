using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PowerTypeCategory { //The standard way of identifying what machinery is
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
	PowerGenerator,
}//hey Be careful when changing this because it's stored as numbers in prefabs/saved scenes for some stupid reason so addon never Change the order 