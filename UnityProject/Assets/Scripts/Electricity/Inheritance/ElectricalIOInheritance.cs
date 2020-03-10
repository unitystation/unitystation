using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using System.Linq;

[System.Serializable]
public class ElectricalOIinheritance : NetworkBehaviour, IServerDespawn
{ //is the Bass class but every node inherits from
	public Connection WireEndB;
	public Connection WireEndA;

	[SerializeField]
	public ElectronicData Data = new ElectronicData();
	[SerializeField]
	public IntrinsicElectronicData InData = new IntrinsicElectronicData();
	public HashSet<ElectricalOIinheritance> connectedDevices = new HashSet<ElectricalOIinheritance>();

	public RegisterTile registerTile;
	public Matrix Matrix => registerTile.Matrix; //This is a bit janky with inheritance
	public bool connected = false;

	public bool Logall = false;
	public bool DestroyQueueing = false;

	private void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (registerTile == null)
		{
			registerTile = GetComponent<RegisterTile>();
		}
		if (registerTile == null)
		{
			Logger.Log("Confused screaming! > " + this.name);
		}
		else {
			registerTile.SetElectricalData(this);
		}

	}

	public override void OnStartClient()
	{
		EnsureInit();
	}

	public override void OnStartServer()
	{
		EnsureInit();
		base.OnStartServer();
		StartCoroutine(WaitForLoad());
	}
	public virtual void _Start()
	{
	}

	IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(1f);
		ElectricalSynchronisation.StructureChange = true;
		FindPossibleConnections();
		InData.Present = this;
	}

	public virtual void FindPossibleConnections()
	{
		Data.connections.Clear();
		if (registerTile != null)
		{
			Data.connections = ElectricityFunctions.FindPossibleConnections(
				Matrix,
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


	public void DestroyThisPlease()
	{
		DestroyQueueing = true;
		ElectricalSynchronisation.NUElectricalObjectsToDestroy.Add(this);
	}

	public virtual GameObject GameObject()
	{
		//FIXME find out why this object has been destroyed?
		//putting in this condition check as returning the null gameobject directly
		//throws many NRE's on the server leading to unwanted behaviour
		if (this == null || this.gameObject == null)
		{
			Logger.Log("The gameobject for this electrical object has been destroyed!!!!!", Category.Electrical);
			return null;
		}
		return gameObject;
	}

	/// <summary>
	///  Sets the upstream
	/// </summary>
	public virtual void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn = null)
	{
		//if (Logall)
		//{
		//	Logger.Log("this > " + this + "DirectionInput SourceInstance > " + SourceInstance + " ComingFrom > " + ComingFrom + "  PassOn > " + PassOn, Category.Electrical);
		//}
		//InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, this);
	}

	/// <summary>
	/// Sets the downstream and pokes the next one along
	/// </summary>
	public virtual void DirectionOutput(GameObject SourceInstance)
	{

		//InputOutputFunctions.DirectionOutput(SourceInstance, this);
		//if (Logall)
		//{
		//	Logger.Log("this > " + this + "DirectionOutput " + this.gameObject + " <ID | Downstream = " + Data.SupplyDependent[SourceInstance].Downstream.Count + " Upstream = " + Data.SupplyDependent[SourceInstanceID].Upstream.Count + "connections " + (string.Join(",", Data.connections)), Category.Electrical);
		//}

	}

	/// <summary>
	/// Pass resistance with GameObject of the Machine it is heading toward
	/// </summary>
	public virtual void ResistanceInput(ResistanceWrap Resistance,
										ElectricalOIinheritance SourceInstance,
										IntrinsicElectronicData ComingFrom,
										List<ElectricalDirectionStep> NetworkPath)
	{
		if (Logall)
		{
			Logger.Log("this > " + this + "ResistanceInput, Resistance > " + Resistance + " SourceInstance  > " + SourceInstance + " ComingFrom > " + ComingFrom, Category.Electrical);
		}
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, NetworkPath, this);
	}

	/// <summary>
	/// Passes it on to the next cable
	/// </summary>
	public virtual void ResistancyOutput(ResistanceWrap Resistance,
										 ElectricalOIinheritance SourceInstance,
										 List<ElectricalDirectionStep> Directions)
	{
		if (Logall)
		{
			Logger.Log("this > " + this + "ResistancyOutput, Resistance > " + Resistance + " SourceInstance  > " + SourceInstance, Category.Electrical);
		}
		InputOutputFunctions.ResistancyOutput(Resistance, Resistance, SourceInstance, Directions, this);
	}

	/// <summary>
	/// Inputs a current from a device, with the supply
	/// </summary>
	public virtual void ElectricityInput(WrapCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 ElectricalOIinheritance ComingFrom,
										 ElectricalDirectionStep Path)
	{
		if (Logall)
		{
			Logger.Log("this > " + this + "ElectricityInput, Current > " + Current + " SourceInstance  > " + SourceInstance + " ComingFrom > " + ComingFrom, Category.Electrical);
		}
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, Path, this);
	}

	/// <summary>
	///The function for out putting current into other nodes (Basically doing ElectricityInput On another one)
	/// </summary>
	public virtual void ElectricityOutput(WrapCurrent Current,
										  ElectricalOIinheritance SourceInstance,
										  ElectricalOIinheritance ComingFrom,
										  ElectricalDirectionStep Path)
	{
		if (Logall)
		{
			Logger.Log("this > " + this + "ElectricityOutput, Current > " + Current + " SourceInstance  > " + SourceInstance, Category.Electrical);
		}
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, ComingFrom, this, Path);

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
		if (InData?.ControllingDevice != null)
		{
			InData.ControllingDevice.PotentialDestroyed();
		}
	}

	/// <summary>
	/// Flushs the resistance and up. Cleans out resistance and current, SourceInstance is the Gameobject Of the supply,
	/// This will be used to clean up the data from only a particular Supply
	/// </summary>
	public virtual void FlushResistanceAndUp(ElectricalOIinheritance SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	}

	/// <summary>
	/// Flushs the supply and up. Cleans out the current
	/// </summary>
	public virtual void FlushSupplyAndUp(ElectricalOIinheritance SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushSupplyAndUp(this, SourceInstance);
	}
	public virtual void RemoveSupply(ElectricalDirectionStep Path, ElectricalOIinheritance SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, Path, SourceInstance);
	}

	[RightClickMethod]
	protected virtual void ShowDetails()
	{
		if (isServer)
		{
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
				ToLog += "SourceVoltages > ";
				ToLog += string.Join(",", Supply.Value.SourceVoltages) + "\n";
				Logger.Log(ToLog, Category.Electrical);
			}
			Logger.Log(" ActualVoltage > " + Data.ActualVoltage + " CurrentInWire > " + Data.CurrentInWire + " EstimatedResistance >  " + Data.EstimatedResistance, Category.Electrical);
		}

		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}


	public void DestroyingThisNow()
	{
		if (DestroyQueueing)
		{
			FlushConnectionAndUp();
			FindPossibleConnections();
			FlushConnectionAndUp();

			registerTile.UnregisterClient();
			registerTile.UnregisterServer();
			if (this != null)
			{
				Despawn.ServerSingle(gameObject);
			}
		}
	}



	/// <summary>
	/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
	/// </summary>
	public void OnDespawnServer(DespawnInfo info)
	{
		if (!DestroyQueueing)
		{
			Logger.Log("REEEEEEEEEEEEEE Wait your turn to destroy, Electrical thread is busy!!");
			DestroyThisPlease();
			var yy = InData.ConnectionReaction[PowerTypeCategory.Transformer];
		}
	}

	//
	//	public RightClickableResult GenerateRightClickOptions()
	//	{
	//		return RightClickableResult.Create()
	//			.AddElement("Details", ShowDetails);
	//	}
}

