using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ElectricalOIinheritance : NetworkBehaviour, IElectricityIO { //is the Bass class but every  node inherits from 
	public int DirectionStart;
	public int DirectionEnd;
	public ElectronicData Data { get; set; } = new ElectronicData();
	public IntrinsicElectronicData InData { get; set; } = new IntrinsicElectronicData();
	public HashSet<IElectricityIO> connectedDevices { get; set; } = new HashSet<IElectricityIO>();

	public RegisterItem registerTile;
	public Matrix matrix => registerTile.Matrix; //This is a bit janky with inheritance 
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

	public virtual void FindPossibleConnections()
	{
		Data.connections.Clear();
		if (registerTile != null) {
			Data.connections = ElectricityFunctions.FindPossibleConnections(
				transform.localPosition,
				matrix,
				InData.CanConnectTo,
				GetConnPoints()
			);
		}

		if (Data.connections.Count > 0)
		{
			connected = true;
		}
	}
		
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

	public virtual void DirectionInput(GameObject SourceInstance, IElectricityIO ComingFrom,  CableLine PassOn  = null)
	{
		InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, this);
	}
	public virtual void DirectionOutput(GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		InputOutputFunctions.DirectionOutput(SourceInstance, this);
		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Data.Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Data.Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}

	public virtual void ResistanceInput(float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, this);
	}

	public virtual void ResistancyOutput(GameObject SourceInstance)
	{

		float Resistance = ElectricityFunctions.WorkOutResistance(Data.ResistanceComingFrom[ SourceInstance.GetInstanceID()]);
		InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, this);
	}
	public virtual void ElectricityInput( float Current, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
	}

	public virtual void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
		Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
		Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
		Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;
	}
	public virtual void SetConnPoints(int DirectionEndin, int DirectionStartin)
	{
		DirectionEnd = DirectionEndin;
		DirectionStart = DirectionStartin;
	}
	public virtual void FlushConnectionAndUp()
	{
		ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
		InData.ControllingDevice.PotentialDestroyed();
	}
	public virtual void FlushResistanceAndUp(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	}
	public virtual void FlushSupplyAndUp(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushSupplyAndUp(this, SourceInstance);
	}
	public virtual void RemoveSupply(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, SourceInstance);
	}

	[ContextMethod("Details", "Magnifying_glass")]
	public virtual void ShowDetails()
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

