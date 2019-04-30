using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
[System.Serializable]
public class InLineDevice : ElectricalOIinheritance
{
	//What is the purpose of inline device, It is to modify current, resistance going over the device E.G a Transformer For any other device that can be thought of
	public ElectricalNodeControl RelatedDevice;

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
		//Not working for some reason:
		registerTile3 = gameObject.GetComponent<RegisterObject>();
		StartCoroutine(WaitForLoad());
		posCache = transform.localPosition;
	}



	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		FindPossibleConnections();
	}


	public void PowerUpdateStructureChange()
	{
		FlushConnectionAndUp();
	}

	public void PowerNetworkUpdate() { }


	public override void ResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		Resistance = RelatedDevice.ModifyResistanceInput(Resistance, SourceInstance, ComingFrom);
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, this);
	}

	public override void ResistancyOutput(GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		float Resistance = ElectricityFunctions.WorkOutResistance(Data.ResistanceComingFrom[SourceInstanceID]);
		Resistance = RelatedDevice.ModifyResistancyOutput( Resistance, SourceInstance);
		InputOutputFunctions.ResistancyOutput( Resistance, SourceInstance, this);
	}

	public override void ElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		Current = RelatedDevice.ModifyElectricityInput( Current, SourceInstance, ComingFrom);
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
	}

	public override void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		if (!(SourceInstance == gameObject)){
			if (!ElectricalSynchronisation.NUCurrentChange.Contains(InData.ControllingDevice)) { 
				ElectricalSynchronisation.NUCurrentChange.Add(InData.ControllingDevice);
			}
		}

		Current = RelatedDevice.ModifyElectricityOutput(Current, SourceInstance);

		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);

		ElectricityFunctions.WorkOutActualNumbers(this);
	}		
}