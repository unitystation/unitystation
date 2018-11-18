using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LowVoltageCable : NetworkBehaviour, ICable
{
	public WiringColor CableType {get; set;} = WiringColor.low;
	public bool IsCable {get; set;}
	public int DirectionEnd {get{ return RelatedWire.DirectionEnd;}set{ RelatedWire.DirectionEnd = value; }}
	public int DirectionStart {get{ return RelatedWire.DirectionStart;} set{ RelatedWire.DirectionStart = value; }}
	public WireConnect RelatedWire;
	public PowerTypeCategory ApplianceType = PowerTypeCategory.LowVoltageCable;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		
		PowerTypeCategory.APC,
		PowerTypeCategory.LowVoltageCable,
	};
	public override void OnStartClient()
	{
		base.OnStartClient();
		CableType = WiringColor.low;
		IsCable = true;
		RelatedWire.CanConnectTo = CanConnectTo;
		RelatedWire.Categorytype = ApplianceType;
	}

	private void OnDisable()
	{
	}

	public void OnDestroy(){
		ElectricalSynchronisation.StructureChangeReact = true;
		ElectricalSynchronisation.ResistanceChange = true;
		ElectricalSynchronisation.CurrentChange = true;
		//Then you can destroy
	}
}
