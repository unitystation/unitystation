using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]

public class ElectricalOIinheritance : NetworkBehaviour, IServerDespawn
{

	//is the Bass class but every node inherits from
	[SerializeField]
	public IntrinsicElectronicData InData = new IntrinsicElectronicData();
	public HashSet<IntrinsicElectronicData> connectedDevices = new HashSet<IntrinsicElectronicData>();

	public RegisterTile registerTile;
	public Matrix Matrix => registerTile.Matrix;
	public bool connected = false;

	public bool Logall = false;

	private void Start()
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
			Logger.LogError("Confused screaming! > " + this.name, Category.Electrical);
		}
		else {
			registerTile.SetElectricalData(this);
			Vector2 searchVec = this.registerTile.LocalPosition.To2Int();
		}
		ElectricalManager.Instance.electricalSync.StructureChange = true;
		InData.Present = this;
	}

	public override void OnStartClient()
	{
		EnsureInit();
	}

	public override void OnStartServer()
	{
		EnsureInit();
		base.OnStartServer();

	}

	public virtual void FindPossibleConnections()
	{
		InData.Data.connections.Clear();
		if (registerTile != null)
		{
			 ElectricityFunctions.FindPossibleConnections(
				Matrix,
				InData.CanConnectTo,
				GetConnPoints(),
				InData,
				InData.Data.connections
			);
		}
	}

	public virtual ConnPoint GetConnPoints()
	{
		return (InData.GetConnPoints());
	}



	/// <summary>
	/// Sets the downstream and pokes the next one along
	/// </summary>
	public virtual void DirectionOutput(ElectricalOIinheritance SourceInstance)
	{

		InputOutputFunctions.DirectionOutput(SourceInstance, InData);
	}

	///// <summary>
	/////  Sets the upstream
	///// </summary>
	public virtual void DirectionInput(ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom, CableLine PassOn = null)
	{
		if (Logall)
		{
			Logger.Log("this > " + this + "DirectionInput SourceInstance > " + SourceInstance + " ComingFrom > " + ComingFrom + "  PassOn > " + PassOn, Category.Electrical);
		}
		InputOutputFunctions.DirectionInput(SourceInstance, ComingFrom, InData);
	}



	/// <summary>
	/// Pass resistance with GameObject of the Machine it is heading toward
	/// </summary>
	public virtual void ResistanceInput(ResistanceWrap Resistance,
										ElectricalOIinheritance SourceInstance,
										IntrinsicElectronicData ComingFrom)
	{
		if (Logall)
		{
			Logger.Log("this > " + this
			           + "ResistanceInput, Resistance > " + Resistance
			           + " SourceInstance  > " + SourceInstance
			           + " ComingFrom > " + ComingFrom, Category.Electrical);
		}
		InputOutputFunctions.ResistanceInput(Resistance, SourceInstance, ComingFrom, InData);
	}

	/// <summary>
	/// Passes it on to the next cable
	/// </summary>
	public virtual void ResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
	{
		if (Logall)
		{
			Logger.Log("this > " + this
			           + "ResistancyOutput, Resistance > " + Resistance
			           + " SourceInstance  > " + SourceInstance, Category.Electrical);
		}
		InputOutputFunctions.ResistancyOutput(Resistance, SourceInstance, InData);
	}

	/// <summary>
	/// Inputs a current from a device, with the supply
	/// </summary>
	public virtual void ElectricityInput(VIRCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 IntrinsicElectronicData ComingFrom)
	{
		if (Logall)
		{
			Logger.Log("this > " + this
			           + "ElectricityInput, Current > " + Current
			           + " SourceInstance  > " + SourceInstance
			           + " ComingFrom > " + ComingFrom, Category.Electrical);
		}
		InputOutputFunctions.ElectricityInput(Current, SourceInstance, ComingFrom, InData);
	}

	/// <summary>
	///The function for out putting current into other nodes (Basically doing ElectricityInput On another one)
	/// </summary>
	public virtual void ElectricityOutput(VIRCurrent Current,
										  ElectricalOIinheritance SourceInstance)
	{
		if (Logall)
		{
			Logger.Log("this > " + this
			           + "ElectricityOutput, Current > " + Current
			           + " SourceInstance  > " + SourceInstance, Category.Electrical);
		}
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, InData);

	}

	public virtual void SetConnPoints(Connection DirectionEndin, Connection DirectionStartin)
	{
		InData.WireEndA = DirectionEndin;
		InData.WireEndB = DirectionStartin;
	}

	/// <summary>
	/// Flushs the connection and up. Flushes out everything
	/// </summary>
	//public virtual void FlushConnectionAndUp()
	//{
	//	ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
	//}

	///// <summary>
	///// Flushs the resistance and up. Cleans out resistance and current, SourceInstance is the Gameobject Of the supply,
	///// This will be used to clean up the data from only a particular Supply
	///// </summary>
	//public virtual void FlushResistanceAndUp(ElectricalOIinheritance SourceInstance = null)
	//{
	//	ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	//}

	///// <summary>
	///// Flushs the supply and up. Cleans out the current
	///// </summary>
	//public virtual void FlushSupplyAndUp(ElectricalOIinheritance SourceInstance = null)
	//{
	//	ElectricalDataCleanup.PowerSupplies.FlushSupplyAndUp(this, SourceInstance);
	//}
	//public virtual void RemoveSupply(ElectricalOIinheritance SourceInstance = null)
	//{
	//	ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, SourceInstance);
	//}

	[RightClickMethod]
	public virtual void ShowDetails()
	{
		if (isServer)
		{
			InData.ShowDetails();
		}

		RequestElectricalStats.Send(PlayerManager.LocalPlayer, gameObject);
	}


	public void DestroyThisPlease()
	{
		InData.DestroyQueueing = true;
		ElectricalManager.Instance.electricalSync.NUElectricalObjectsToDestroy.Add(InData);
	}

	public void DestroyingThisNow()
	{
		if (InData.DestroyQueueing)
		{
			InData.FlushConnectionAndUp();
			FindPossibleConnections();
			InData.FlushConnectionAndUp();

			registerTile.UnregisterClient();
			registerTile.UnregisterServer();
			if (this != null)
			{
				ElectricalManager.Instance.electricalSync.StructureChange = true;
				InData.DestroyAuthorised = true;
				Despawn.ServerSingle(gameObject);
			}
		}
	}



	/// <summary>
	/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
	/// </summary>
	public void OnDespawnServer(DespawnInfo info)
	{
		if (!InData.DestroyQueueing || !InData.DestroyAuthorised)
		{
			Logger.Log("REEEEEEEEEEEEEE Wait your turn to destroy, Electrical thread is busy!!");
			DestroyThisPlease();
			var yy = InData.ConnectionReaction[PowerTypeCategory.Transformer];
		}
	}

	[RightClickMethod]
	public void StructureChange() {
		ElectricalManager.Instance.electricalSync.StructureChange = true;
	}

	//
	//	public RightClickableResult GenerateRightClickOptions()
	//	{
	//		return RightClickableResult.Create()
	//			.AddElement("Details", ShowDetails);
	//	}
}

