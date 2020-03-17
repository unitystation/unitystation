using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
[System.Serializable]
public class InLineDevice : ElectricalOIinheritance
{
	//What is the purpose of inline device, It is to modify current, resistance going over the device E.G a Transformer For any other device that can be thought of


	public override void ResistanceInput(ResistanceWrap Resistance, 
	                                     ElectricalOIinheritance SourceInstance, 
	                                     IntrinsicElectronicData ComingFrom, 
	                                     List<ElectricalDirectionStep> NetworkPath)
	{
		Resistance = InData.ControllingDevice.ModifyResistanceInput(Resistance, SourceInstance, ComingFrom);
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, NetworkPath, this);
	}

	public override void ResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance, List<ElectricalDirectionStep> Directions)
	{
		var unResistance = Resistance;
		Resistance = InData.ControllingDevice.ModifyResistancyOutput( Resistance, SourceInstance);
		InputOutputFunctions.ResistancyOutput( Resistance, unResistance,  SourceInstance, Directions,  this);
	}

	public override void ElectricityInput(WrapCurrent Current,
	                                      ElectricalOIinheritance SourceInstance, 
	                                      ElectricalOIinheritance ComingFrom,
	                                      ElectricalDirectionStep Path)
	{
		Current.SendingCurrent = InData.ControllingDevice.ModifyElectricityInput( Current.SendingCurrent, SourceInstance, ComingFrom);
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom,  Path, this);
	}

	public override void ElectricityOutput(WrapCurrent Current, ElectricalOIinheritance SourceInstance, ElectricalOIinheritance ComingFrom,  ElectricalDirectionStep Path)
	{
		//Logger.Log("inline > " + Current);	
		Current.SendingCurrent = InData.ControllingDevice.ModifyElectricityOutput(Current.SendingCurrent, SourceInstance);
		//Logger.Log("inline1 > " + Current);
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance,ComingFrom, this, Path);
		//Logger.Log("inline2 > " + Current);
		ElectricityFunctions.WorkOutActualNumbers(this);
	}		
}