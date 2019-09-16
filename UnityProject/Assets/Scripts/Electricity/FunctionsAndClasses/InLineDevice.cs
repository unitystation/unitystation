using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
[System.Serializable]
public class InLineDevice : ElectricalOIinheritance
{
	//What is the purpose of inline device, It is to modify current, resistance going over the device E.G a Transformer For any other device that can be thought of

	public RegisterObject registerTile3;
	private Matrix matrix => registerTile3.Matrix;
	private Vector3 posCache;
	private bool isSupplying = false;

	public override void FindPossibleConnections()
	{
		Data.connections.Clear();
		Data.connections = ElectricityFunctions.FindPossibleConnections(
			transform.localPosition,
			matrix,
			InData.CanConnectTo,
			GetConnPoints(),
			this
		);
		if (Data.connections.Count > 0)
		{
			connected = true;
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		InData.ElectricityOverride = true;
		InData.ResistanceOverride = true;
		//Not working for some reason:
		registerTile3 = gameObject.GetComponent<RegisterObject>();
		StartCoroutine(WaitForLoad());
		posCache = transform.localPosition;
	}

	IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(2f);
		FindPossibleConnections();
	}

	public override void ResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		Resistance = InData.ControllingDevice.ModifyResistanceInput(Resistance, SourceInstance, ComingFrom);
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, this);
	}

	public override void ResistancyOutput(GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		float Resistance = ElectricityFunctions.WorkOutResistance(Data.SupplyDependent[SourceInstanceID].ResistanceComingFrom);
		Resistance = InData.ControllingDevice.ModifyResistancyOutput( Resistance, SourceInstance);
		InputOutputFunctions.ResistancyOutput( Resistance, SourceInstance, this);
	}

	public override void ElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		Current = InData.ControllingDevice.ModifyElectricityInput( Current, SourceInstance, ComingFrom);
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
	}

	public override void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		Current = InData.ControllingDevice.ModifyElectricityOutput(Current, SourceInstance);
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
		ElectricityFunctions.WorkOutActualNumbers(this);
	}		
}