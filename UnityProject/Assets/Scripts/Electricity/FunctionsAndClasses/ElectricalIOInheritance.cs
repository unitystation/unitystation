using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
[System.Serializable]
public class ElectricalOIinheritance : NetworkBehaviour { //is the Bass class but every  node inherits from 
	public Connection WireEndB;
	public Connection WireEndA;

	[SerializeField]
	public ElectronicData Data = new ElectronicData();
	[SerializeField]
	public IntrinsicElectronicData InData = new IntrinsicElectronicData();
	public HashSet<ElectricalOIinheritance> connectedDevices  = new HashSet<ElectricalOIinheritance>();

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
		//FindPossibleConnections();
		//FlushConnectionAndUp();
		//FindPossibleConnections();
	}
	public virtual void _Start()
	{
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
				GetConnPoints(),
				this
			);
		}

		if (Data.connections.Count > 0)
		{
			connected = true;
		}
	}
		
	public virtual ConnPoint GetConnPoints()
	{
		ConnPoint conns = new ConnPoint();
		conns.pointA = WireEndB;
		conns.pointB = WireEndA;
		return conns;
	}

	public Connection InputPosition()
	{
		return WireEndB;
	}

	public Connection OutputPosition()
	{
		return WireEndA;
	}

	public virtual GameObject GameObject()
	{
		return gameObject;
	}

	/// <summary>
	///  Sets the upstream 
	/// </summary>
	public virtual void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom,  CableLine PassOn  = null)
	{
		InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, this);
	}

	/// <summary>
	/// Sets the downstream and pokes the next one along 
	/// </summary>
	public virtual void DirectionOutput(GameObject SourceInstance)
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
	public virtual void ResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, this);
	}

	/// <summary>
	/// Passes it on to the next cable
	/// </summary>
	public virtual void ResistancyOutput(GameObject SourceInstance)
	{
		float Resistance = ElectricityFunctions.WorkOutResistance(Data.SupplyDependent[SourceInstance.GetInstanceID()].ResistanceComingFrom);
		InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, this);
	}

	/// <summary>
	/// Inputs a current from a device, with the supply
	/// </summary>
	public virtual void ElectricityInput( float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, this);
	}

	/// <summary>
	///The function for out putting current into other nodes (Basically doing ElectricityInput On another one)
	/// </summary>
	public virtual void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		//SourceInstance.GetComponent<ElectricalOIinheritance>();
		//Logger.Log("oh man?");
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);

	}

	public virtual void SetConnPoints(Connection DirectionEndin, Connection DirectionStartin)
	{
		WireEndA = DirectionEndin;
		WireEndB = DirectionStartin;
	}

	/// <summary>
	/// Flushs the connection and up. Flushes out everything
	/// </summary>
	public virtual void FlushConnectionAndUp()
	{
		ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
		InData.ControllingDevice.PotentialDestroyed();
	}

	/// <summary>
	/// Flushs the resistance and up. Cleans out resistance and current 
	/// </summary>
	public virtual void FlushResistanceAndUp(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	}

	/// <summary>
	/// Flushs the supply and up. Cleans out the current 
	/// </summary>
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
		{ //(string.Join(",", Data.connection))
			ElectricityFunctions.WorkOutActualNumbers(this);
			Logger.Log("connections " + (string.Join(",", Data.connections)), Category.Electrical);
			Logger.Log("ID " + (this.GetInstanceID()), Category.Electrical);
			Logger.Log("Type " + (InData.Categorytype.ToString()), Category.Electrical);
			Logger.Log("Can connect to " + (string.Join(",", InData.CanConnectTo)), Category.Electrical);
			foreach (var Supply in Data.SupplyDependent)
			{
				string ToLog;
				ToLog = "Supply > " + Supply.Key + "\n";
				ToLog += "Upstream > ";
				ToLog += string.Join(",", Supply.Value.Upstream) + "\n";
				ToLog += "Downstream > ";
				ToLog += string.Join(",", Supply.Value.Downstream) + "\n";
				ToLog += "ResistanceGoingTo > ";
				ToLog += string.Join(",", Supply.Value.ResistanceGoingTo) + "\n";
				ToLog += "ResistanceComingFrom > ";
				ToLog += string.Join(",", Supply.Value.ResistanceComingFrom) + "\n";
				ToLog += "CurrentComingFrom > ";
				ToLog += string.Join(",", Supply.Value.CurrentComingFrom) + "\n";
				ToLog += "CurrentGoingTo > ";
				ToLog += string.Join(",", Supply.Value.CurrentGoingTo) + "\n";
				ToLog += Supply.Value.SourceVoltages.ToString();
				Logger.Log(ToLog, Category.Electrical);
			}
			Logger.Log(" ActualVoltage > " + Data.ActualVoltage + " CurrentInWire > " + Data.CurrentInWire + " EstimatedResistance >  " + Data.EstimatedResistance, Category.Electrical);
		}

		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}
}

