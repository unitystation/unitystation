using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
[System.Serializable]
public class InLineDevice : ElectricalOIinheritance
{
	//What is the purpose of inline device, It is to modify current, resistance going over the device E.G a Transformer For any other device that can be thought of


	public override void ResistanceInput(VIRResistances Resistance,
										ElectricalOIinheritance SourceInstance,
										IntrinsicElectronicData ComingFrom)
	{
		Resistance = InData.ControllingDevice.ModifyResistanceInput(Resistance, SourceInstance, ComingFrom);


		if (Logall)
		{
			Logger.Log("this > " + this + "ResistanceInput, Resistance > " + Resistance + " SourceInstance  > " + SourceInstance + " ComingFrom > " + ComingFrom, Category.Electrical);
		}
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, this);
	}

	public override void ResistancyOutput(ElectricalOIinheritance SourceInstance)
	{
		VIRResistances Resistance = ElectricityFunctions.WorkOutVIRResistance(Data.SupplyDependent[SourceInstance].ResistanceComingFrom);
		Resistance = InData.ControllingDevice.ModifyResistancyOutput( Resistance, SourceInstance );
		if (Logall)
		{
			Logger.Log("this > " + this + "ResistancyOutput, Resistance > " + Resistance + " SourceInstance  > " + SourceInstance, Category.Electrical);
		}
		InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, this);
	}

	public override void ElectricityInput(VIRCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 ElectricalOIinheritance ComingFrom)
	{
		Current = InData.ControllingDevice.ModifyElectricityInput( Current, SourceInstance, ComingFrom);

		if (Logall)
		{
			Logger.Log("this > " + this + "ElectricityInput, Current > " + Current + " SourceInstance  > " + SourceInstance + " ComingFrom > " + ComingFrom, Category.Electrical);
		}
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
	}

	public override void ElectricityOutput(VIRCurrent Current,
										  ElectricalOIinheritance SourceInstance)
	{
		
		Current = InData.ControllingDevice.ModifyElectricityOutput(Current, SourceInstance);

		if (Logall)
		{
			Logger.Log("this > " + this + "ElectricityOutput, Current > " + Current + " SourceInstance  > " + SourceInstance, Category.Electrical);
		}
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
	}		
}