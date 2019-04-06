using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
[System.Serializable]
public class ElectricalOIinheritance : NetworkBehaviour { //is the Bass class but every  node inherits from 
	public int DirectionStart;
	public int DirectionEnd;

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
	}

	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(1f);
		FindPossibleConnections();
	}

	public virtual void FindPossibleConnections()
	{
		//Logger.Log(InData.CanConnectTo.Count.ToString() + "owoeeeeeee");
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
		
	public virtual ConnPoint GetConnPoints()
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
		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
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

		float Resistance = ElectricityFunctions.WorkOutResistance(Data.ResistanceComingFrom[ SourceInstance.GetInstanceID()]);
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
		Logger.Log("oh man?");
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
		ElectricityFunctions.WorkOutActualNumbers(this);
	}

	/// <summary>
	///     Returns a struct with both connection points as members
	///     the connpoint connection positions are represented using 4 bits to indicate N S E W - 1 2 4 8
	///     Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
	///     This is the edge of the location where the input connection enters the turf
	///     Use 0 for Machines or grills that can conduct electricity from being placed ontop of any wire configuration
	/// </summary>
	public virtual void SetConnPoints(int DirectionEndin, int DirectionStartin)
	{
		DirectionEnd = DirectionEndin;
		DirectionStart = DirectionStartin;
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

