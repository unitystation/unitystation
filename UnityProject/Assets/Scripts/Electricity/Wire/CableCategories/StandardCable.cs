using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StandardCable : NetworkBehaviour, ICable, IDeviceControl
{
	private bool SelfDestruct = false;

	public WiringColor CableType { get; set; } = WiringColor.red;
	public bool IsCable { get; set; } = true;
	public int DirectionEnd { get { return wireConnect.DirectionEnd; } set { wireConnect.DirectionEnd = value; } }
	public int DirectionStart { get { return wireConnect.DirectionStart; } set { wireConnect.DirectionStart = value; } }
	private WireConnect wireConnect;
	public PowerTypeCategory ApplianceType = PowerTypeCategory.StandardCable;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.StandardCable,
			PowerTypeCategory.FieldGenerator,
			PowerTypeCategory.SMES,
			PowerTypeCategory.Transformer,
			PowerTypeCategory.DepartmentBattery,
			PowerTypeCategory.MediumMachineConnector,
		PowerTypeCategory.PowerGenerator
	};

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	void Awake()
	{
		wireConnect = GetComponent<WireConnect>();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		CableType = WiringColor.red;
		IsCable = true;
		wireConnect.InData.CanConnectTo = CanConnectTo;
		wireConnect.InData.Categorytype = ApplianceType;
		wireConnect.InData.ControllingDevice = this;
	}

	//FIXME: Objects at runtime do not get destroyed. Instead they are returned back to pool
	//FIXME: that also renderers IDevice useless. Please reassess
	public void OnDestroy()
	{
//		ElectricalSynchronisation.StructureChangeReact = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		SelfDestruct = true;
		//Making Invisible
	}
	public void TurnOffCleanup (){
	}
}