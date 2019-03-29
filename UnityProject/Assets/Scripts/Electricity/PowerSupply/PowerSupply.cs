using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class PowerSupply : ElectricalOIinheritance, IElectricalNeedUpdate, IElectricityIO, IProvidePower
{
	public RegisterObject registerTile3;
	private Matrix matrix => registerTile3.Matrix;
	private Vector3 posCache;
	private bool isSupplying = false;

	public IElectricityIO _IElectricityIO { get; set; }

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
		InData.ElectricityOverride = true;
		_IElectricityIO = this.gameObject.GetComponent<IElectricityIO>();
		//Not working for some reason:
		registerTile3 = gameObject.GetComponent<RegisterObject>();
		StartCoroutine(WaitForLoad());
		posCache = transform.localPosition;
		UpdateManager.Instance.Add(UpdateMe);
	}

	public void OnDisable()
	{
		if (isServer)
		{
			if (UpdateManager.Instance != null)
			{
				UpdateManager.Instance.Remove(UpdateMe);
			}
		}
	}

	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		FindPossibleConnections();
	}

	//UpdateManager on server only
	void UpdateMe()
	{
		if (posCache != transform.localPosition)
		{
			posCache = transform.localPosition;
			//if (isSupplying)
			//{
				TurnOffSupply();
			//}
			FindPossibleConnections();
		}
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
	public void InitialPowerUpdateResistance(){
		
	}

	public void PowerUpdateResistanceChange()
	{

	}
	public void PowerUpdateCurrentChange()
	{
		PowerSupplyFunction.PowerUpdateCurrentChange (this);
	}

	public void PowerNetworkUpdate()
	{

	}

	public override void ResistancyOutput( GameObject SourceInstance)
	{
		//Logger.Log (SourceInstance.GetInstanceID().ToString() + " < Receive | is > " + this.gameObject.GetInstanceID().ToString() );
		if (!(SourceInstance == this.gameObject))
		{
			int SourceInstanceID = SourceInstance.GetInstanceID();
			float Resistance = ElectricityFunctions.WorkOutResistance(Data.ResistanceComingFrom[SourceInstanceID]);
			InputOutputFunctions.ResistancyOutput( Resistance, SourceInstance, this);
		}
	}

	public override void SetConnPoints(int DirectionEndin, int DirectionStartin) { }

	public override void ElectricityOutput( float Current, GameObject SourceInstance)
	{
		if (!(SourceInstance == this.gameObject))
		{
			if (!ElectricalSynchronisation.NUCurrentChange.Contains(InData.ControllingUpdate))
			{
				ElectricalSynchronisation.NUCurrentChange.Add(InData.ControllingUpdate);
			}

		}
		InputOutputFunctions.ElectricityOutput(Current, SourceInstance, this);
		ElectricityFunctions.WorkOutActualNumbers(this);
	}
}