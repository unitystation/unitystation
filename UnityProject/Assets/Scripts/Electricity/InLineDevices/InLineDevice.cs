using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class InLineDevice : ElectricalOIinheritance, IElectricityIO, IProvidePower
{
	//What is the purpose of inline device, It is to modify current, resistance going over the device E.G a Transformer For any other device that can be thought of
	public IInLineDevices RelatedDevice;

	public HashSet<IElectricityIO> DirectionWorkOnNextList { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> DirectionWorkOnNextListWait { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> ResistanceWorkOnNextList { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> ResistanceWorkOnNextListWait { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> connectedDevices { get; set; } = new HashSet<IElectricityIO>();

	public RegisterObject registerTile3;
	private Matrix matrix => registerTile3.Matrix;
	private Vector3 posCache;
	private bool isSupplying = false;

	public void FindPossibleConnections()
	{
		Data.connections.Clear();
		Data.connections = ElectricityFunctions.FindPossibleConnections(
			transform.localPosition,
			matrix,
			InData.CanConnectTo,
			GetConnPoints()
		);
		if (Data.connections.Count > 0)
		{
			connected = true;
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
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

	public void TurnOnSupply()
	{
		PowerSupplyFunction.TurnOnSupply (this);
	}

	public void TurnOffSupply()
	{
		PowerSupplyFunction.TurnOffSupply (this);
	}

	public void PowerUpdateStructureChange()
	{
		FlushConnectionAndUp();
	}

	public void PowerUpdateStructureChangeReact()
	{
		PowerSupplyFunction.PowerUpdateStructureChangeReact (this);
	}

	public void InitialPowerUpdateResistance(){}
	public void PowerUpdateResistanceChange()
	{
	}
	public void PowerUpdateCurrentChange()
	{
		PowerSupplyFunction.PowerUpdateCurrentChange (this);
	}

	public void PowerNetworkUpdate() { }


	public override void ResistanceInput(float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom)
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

	public override void ElectricityInput(float Current, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		Current = RelatedDevice.ModifyElectricityInput( Current, SourceInstance, ComingFrom);
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
	}

	public override void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		if (!(SourceInstance == this.gameObject)){
			ElectricalSynchronisation.NUCurrentChange.Add(InData.ControllingUpdate);
		}

		Current = RelatedDevice.ModifyElectricityOutput(Current, SourceInstance);
		//Logger.Log (CurrentInWire.ToString () + " How much current", Category.Electrical);
		if (Current != 0)
		{
			InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
		}
		Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
		Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
		Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;
	}

	public void SetConnPoints(int DirectionEndin, int DirectionStartin) { }

		
}