using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class HighVoltageCable : NetworkBehaviour, ICable
{
	public WiringColor CableType {get; set;} = WiringColor.high;
	public bool IsCable {get; set;} = true;
	public int DirectionEnd {get{ return RelatedWire.DirectionEnd;}set{ RelatedWire.DirectionEnd = value; }}
	public int DirectionStart {get{ return RelatedWire.DirectionStart;} set{ RelatedWire.DirectionStart = value; }}
	public WireConnect RelatedWire;
	public PowerTypeCategory ApplianceType = PowerTypeCategory.HighVoltageCable;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.HighVoltageCable,
		PowerTypeCategory.Transformer,
	};
	public override void OnStartClient()
	{
		base.OnStartClient();
		CableType = WiringColor.high;
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
