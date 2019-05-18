using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComplexElectricalIOIntermediate : ElectricalOIinheritance
{
	public ElectricalComplexNodeControl ComplexPassOver;
	/// <summary>
	///  Sets the upstream 
	/// </summary>
	public override void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn = null)
	{
		InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, this);
	}

	/// <summary>
	/// Sets the downstream and pokes the next one along 
	/// </summary>
	public override void DirectionOutput(GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		InputOutputFunctions.DirectionOutput(SourceInstance, this);
		//Data.DownstreamCount = Data.SupplyDependent[SourceInstanceID].Downstream.Count;
		//Data.UpstreamCount = Data.SupplyDependent[SourceInstanceID].Upstream.Count;
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Data.Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Data.Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}

	/// <summary>
	/// Pass resistance with GameObject of the Machine it is heading toward
	/// </summary>
	public override void ResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, this);
	}

	/// <summary>
	/// Passes it on to the next cable
	/// </summary>
	public override void ResistancyOutput(GameObject SourceInstance)
	{
		float Resistance = ElectricityFunctions.WorkOutResistance(Data.SupplyDependent[SourceInstance.GetInstanceID()].ResistanceComingFrom);
		InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, this);
	}

	/// <summary>
	/// Inputs a current from a device, with the supply
	/// </summary>
	public override void ElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
	}

	/// <summary>
	///The function for out putting current into other nodes (Basically doing ElectricityInput On another one)
	/// </summary>
	public override void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
	}
}
