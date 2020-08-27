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
	SolarPanel,
	SolarPanelController,
	PowerSink,
	Turbine,
	VoltageProbe,
	WaterPump,

}//hey Be careful when changing this because it's stored as numbers in prefabs/saved scenes for some stupid reason so addon never Change the order

public enum ElectricalUpdateTypeCategory
{
	OnStartServer,
	ModifyResistancyOutput,
	ModifyResistanceInput,
	ModifyElectricityOutput,
	ModifyElectricityInput,
	PowerNetworkUpdate,
	PowerUpdateCurrentChange,
	PowerUpdateResistanceChange,
	InitialPowerUpdateResistance,
	PowerUpdateStructureChangeReact,
	PowerUpdateStructureChange,
	TurnOffCleanup,
	TurnOnSupply,
	TurnOffSupply,
	PotentialDestroyed,
	GoingOffStage,
	ObjectStateChange,
}

public enum ElectricalModuleTypeCategory
{
	Transformer,
	ResistanceSource,
	SupplyingDevice,
	BatterySupplyingDevice,
}