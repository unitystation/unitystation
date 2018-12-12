using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class PoweredDevice : NetworkBehaviour, IElectricityIO
{

	//Renderers:
	public SpriteRenderer NorthConnection;
	public SpriteRenderer SouthConnection;
	public SpriteRenderer WestConnection;
	public SpriteRenderer EastConnection;

	public int DirectionStart;
	public int DirectionEnd;
	public ElectronicData Data { get; set; } = new ElectronicData();
	public IntrinsicElectronicData InData { get; set; } = new IntrinsicElectronicData();
	public HashSet<IElectricityIO> connectedDevices { get; set; } = new HashSet<IElectricityIO>();
	public RegisterObject registerTile;
	private Matrix matrix => registerTile.Matrix;
	public bool connected = false;
	public bool IsConnector = false;

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
			//			if (IsConnector) { //For connectors sprites
			//				for (int i = 0; i < Data.connections.Count; i++) {
			//					if (Data.connections [i].InData.Categorytype == 0) {
			//					}
			//				}
			//			}
		}
	}
	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO PassOn = null)
	{
		InputOutputFunctions.DirectionInput(tick, SourceInstance, ComingFrom, this);
	}
	public void DirectionOutput(int tick, GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
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
		Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
		Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
		Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;
	}
	public void SetConnPoints(int DirectionEndin, int DirectionStartin)
	{ }
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
			Logger.Log("Can connect to " + (string.Join(",", InData.CanConnectTo)), Category.Electrical);
			Logger.Log("UpstreamCount " + (Data.UpstreamCount.ToString()), Category.Electrical);
			Logger.Log("DownstreamCount " + (Data.DownstreamCount.ToString()), Category.Electrical);
			Logger.Log ("Type " + (InData.Categorytype.ToString()), Category.Electrical);
		}
		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}
}