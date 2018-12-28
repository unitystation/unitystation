using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LowVoltageCable : NetworkBehaviour, ICable, IDeviceControl
{
	private bool SelfDestruct = false;

//	public Sprite NorthSprite;
//	public Sprite SouthSprite;
//	public Sprite WestSprite;
//	public Sprite EastSprite;

	public WiringColor CableType {get; set;} = WiringColor.low;
	public bool IsCable {get; set;}
	public int DirectionEnd {get{ return RelatedWire.DirectionEnd;}set{ RelatedWire.DirectionEnd = value; }}
	public int DirectionStart {get{ return RelatedWire.DirectionStart;} set{ RelatedWire.DirectionStart = value; }}
	public WireConnect RelatedWire;
	public PowerTypeCategory ApplianceType = PowerTypeCategory.LowVoltageCable;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		
		PowerTypeCategory.LowMachineConnector,
		PowerTypeCategory.LowVoltageCable,
	};
	public void PotentialDestroyed(){
		if (SelfDestruct) {
			//Then you can destroy
		}
	}


	public override void OnStartServer()
	{
		base.OnStartServer();
		CableType = WiringColor.low;
		IsCable = true;
		RelatedWire.InData.CanConnectTo = CanConnectTo;
		RelatedWire.InData.Categorytype = ApplianceType;
		RelatedWire.InData.ControllingDevice = this;
	}

	//FIXME:
	public void OnDestroy(){
//		ElectricalSynchronisation.StructureChangeReact = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		SelfDestruct = true;

		//Make Invisible
	}
	public void TurnOffCleanup (){
	}
}
