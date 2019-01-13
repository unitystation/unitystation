using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class WireConnect : NetworkBehaviour, IElectricityIO
{
	public int DirectionStart;
	public int DirectionEnd;
	public ElectronicData Data { get; set; } = new ElectronicData();
	public IntrinsicElectronicData InData { get; set; } = new IntrinsicElectronicData();
	public HashSet<IElectricityIO> connectedDevices { get; set; } = new HashSet<IElectricityIO>();
	public RegisterItem registerTile;
	private Matrix matrix => registerTile.Matrix;
	public bool connected = false;

	public override void OnStartClient()
	{
		base.OnStartClient();
		registerTile = gameObject.GetComponent<RegisterItem>();

	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		StartCoroutine(WaitForLoad());
	}

	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(1f);
		FindPossibleConnections();
	}

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

	// void OnDrawGizmos()
	// {
	// 	if (connected)
	// 	{
	// 		Gizmos.color = Color.yellow;
	// 		Gizmos.DrawSphere(transform.position, 0.1f);
	// 	}
	// }

	public ConnPoint GetConnPoints()
	{
		ConnPoint conns = new ConnPoint();
		conns.pointA = DirectionStart;
		conns.pointB = DirectionEnd;
		return conns;
	}

	public int InputPosition()
	{
		return DirectionStart;
	}

	public int OutputPosition()
	{
		return DirectionEnd;
	}

	public GameObject GameObject()
	{
		return gameObject;
	}

	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO PassOn = null)
	{
		InputOutputFunctions.DirectionInput(tick, SourceInstance, ComingFrom, this);
	}
	public void DirectionOutput(int tick, GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		InputOutputFunctions.DirectionOutput(tick, SourceInstance, this);
		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}

	public void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		InputOutputFunctions.ResistanceInput(tick, Resistance, SourceInstance, ComingFrom, this);
	}

	public void ResistancyOutput(int tick, GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		float Resistance = ElectricityFunctions.WorkOutResistance(Data.ResistanceComingFrom[SourceInstanceID]);
		InputOutputFunctions.ResistancyOutput(tick, Resistance, SourceInstance, this);
	}
	public void ElectricityInput(int tick, float Current, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		InputOutputFunctions.ElectricityInput(tick, Current, SourceInstance, ComingFrom, this);
	}

	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance)
	{
		InputOutputFunctions.ElectricityOutput(tick, Current, SourceInstance, this);
		Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
		Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
		Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;
	}
	public void SetConnPoints(int DirectionEndin, int DirectionStartin)
	{
		DirectionEnd = DirectionEndin;
		DirectionStart = DirectionStartin;
	}
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
			Logger.Log("ActualVoltage " + (Data.ActualVoltage.ToString()), Category.Electrical);
			Logger.Log("CurrentInWire " + (Data.CurrentInWire.ToString()), Category.Electrical);
			Logger.Log("EstimatedResistance " + (Data.EstimatedResistance.ToString()), Category.Electrical);
		}

		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}
}