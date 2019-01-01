using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class InLineDevice : NetworkBehaviour, IElectricityIO, IProvidePower
{
	//What is the purpose of inline device, It is to modify current, resistance going over the device E.G a Transformer For any other device that can be thought of
	public int DirectionStart;
	public int DirectionEnd;

	public ElectronicData Data { get; set; } = new ElectronicData();
	public IntrinsicElectronicData InData { get; set; } = new IntrinsicElectronicData();

	public HashSet<IElectricityIO> DirectionWorkOnNextList { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> DirectionWorkOnNextListWait { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> ResistanceWorkOnNextList { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> ResistanceWorkOnNextListWait { get; set; } = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> connectedDevices { get; set; } = new HashSet<IElectricityIO>();

	public RegisterObject registerTile;
	private Matrix matrix => registerTile.Matrix;
	public bool connected = false;

	public IInLineDevices RelatedDevice;

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
		//registerTile = gameObject.GetComponent<RegisterItem>();
		StartCoroutine(WaitForLoad());
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

	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO PassOn = null)
	{
		InputOutputFunctions.DirectionInput(tick, SourceInstance, ComingFrom, this);
	}
	public void DirectionOutput(int tick, GameObject SourceInstance)
	{
		InputOutputFunctions.DirectionOutput(tick, SourceInstance, this);
		int SourceInstanceID = SourceInstance.GetInstanceID();
		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}

	public void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		Resistance = RelatedDevice.ModifyResistanceInput(tick, Resistance, SourceInstance, ComingFrom);
		InputOutputFunctions.ResistanceInput(tick, Resistance, SourceInstance, ComingFrom, this);
	}

	public void ResistancyOutput(int tick, GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		float Resistance = ElectricityFunctions.WorkOutResistance(Data.ResistanceComingFrom[SourceInstanceID]);
		Resistance = RelatedDevice.ModifyResistancyOutput(tick, Resistance, SourceInstance);
		InputOutputFunctions.ResistancyOutput(tick, Resistance, SourceInstance, this);
	}

	public void ElectricityInput(int tick, float Current, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		Current = RelatedDevice.ModifyElectricityInput(tick, Current, SourceInstance, ComingFrom);
		InputOutputFunctions.ElectricityInput(tick, Current, SourceInstance, ComingFrom, this);
	}

	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance)
	{
		Current = RelatedDevice.ModifyElectricityOutput(tick, Current, SourceInstance);
		//Logger.Log (CurrentInWire.ToString () + " How much current", Category.Electrical);
		if (Current != 0)
		{
			InputOutputFunctions.ElectricityOutput(tick, Current, SourceInstance, this);
		}
		Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
		Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
		Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;
	}

	public void SetConnPoints(int DirectionEndin, int DirectionStartin) { }

	public GameObject GameObject()
	{
		return gameObject;
	}

	public ConnPoint GetConnPoints()
	{
		ConnPoint points = new ConnPoint();
		points.pointA = DirectionStart;
		points.pointB = DirectionEnd;
		return points;
	}

	//Cleans up the values for the specified level 
	public void FlushConnectionAndUp()
	{
		ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
		InData.ControllingDevice.PotentialDestroyed();
	}
	public void FlushResistanceAndUp(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	}
	public void FlushSupplyAndUp(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushSupplyAndUp(this, SourceInstance);
	}
	public void RemoveSupply(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, SourceInstance);
	}

	[ContextMethod("Details", "Magnifying_glass")]
	public void ShowDetails()
	{
		if (isServer)
		{
			Logger.Log("connections " + (Data.connections.Count.ToString()), Category.Electrical);
			Logger.Log("ID " + (this.GetInstanceID()), Category.Electrical);
			Logger.Log("Type " + (InData.Categorytype.ToString()), Category.Electrical);
			Logger.Log("Can connect to " + (string.Join(",", InData.CanConnectTo)), Category.Electrical);
			Logger.Log("UpstreamCount " + (Data.UpstreamCount.ToString()), Category.Electrical);
			Logger.Log("DownstreamCount " + (Data.DownstreamCount.ToString()), Category.Electrical);
		}

		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}
}