using System;
using System.Collections;
using System.Collections.Generic;
using Logs;

namespace Systems.Electricity
{
	/// <summary>
	/// Tthe purpose of InLineDevice is to modify current, resistance going over the device e.g. a transformer or some other electrical device
	/// </summary>
	[Serializable]
	public class InLineDevice : ElectricalOIinheritance
	{
		public override void ResistanceInput(ResistanceWrap Resistance,
											ElectricalOIinheritance SourceInstance,
											IntrinsicElectronicData ComingFrom)
		{

			Resistance = InData.ControllingDevice.ModifyResistanceInput(Resistance, SourceInstance, ComingFrom);
			if (Logall)
			{
				Loggy.Log("this > " + this + "ResistanceInput, Resistance > " + Resistance + " SourceInstance  > " + SourceInstance + " ComingFrom > " + ComingFrom, Category.Electrical);
			}
			InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, InData);
		}

		public override void ResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
		{
			Resistance = InData.ControllingDevice.ModifyResistancyOutput(Resistance, SourceInstance);
			if (Logall)
			{
				Loggy.Log("this > " + this + "ResistancyOutput, Resistance > " + Resistance + " SourceInstance  > " + SourceInstance, Category.Electrical);
			}
			InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, InData);
		}

		public override void ElectricityInput(VIRCurrent Current,
											 ElectricalOIinheritance SourceInstance,
											 IntrinsicElectronicData ComingFrom)
		{
			Current = InData.ControllingDevice.ModifyElectricityInput(Current, SourceInstance, ComingFrom);

			if (Logall)
			{
				Loggy.Log("this > " + this + "ElectricityInput, Current > " + Current + " SourceInstance  > " + SourceInstance + " ComingFrom > " + ComingFrom, Category.Electrical);
			}
			InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, InData);
		}

		public override void ElectricityOutput(VIRCurrent Current,
											  ElectricalOIinheritance SourceInstance)
		{
			Current = InData.ControllingDevice.ModifyElectricityOutput(Current, SourceInstance);
			if (Logall)
			{
				Loggy.Log("this > " + this + "ElectricityOutput, Current > " + Current + " SourceInstance  > " + SourceInstance, Category.Electrical);
			}
			InputOutputFunctions.ElectricityOutput(Current, SourceInstance, InData);
		}
	}
}
