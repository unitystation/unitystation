using System.Collections;
using System.Collections.Generic;

namespace Systems.Electricity
{
	/// <summary>
	/// Meant to be an intermediary for long stretches of cable so as to reduce processing time on long cables 
	/// </summary>
	public class CableLine
	{ 
		public IntrinsicElectronicData InitialGenerator;
		public IntrinsicElectronicData TheStart;
		public IntrinsicElectronicData TheEnd;
		public HashSet<IntrinsicElectronicData> Covering = new HashSet<IntrinsicElectronicData>();

		//public void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn  = null){
		//	JumpToOtherEnd (SourceInstance, ComingFrom);
		//}

		//public void JumpToOtherEnd(GameObject SourceInstance,ElectricalOIinheritance ComingFrom){
		//	if (ComingFrom == TheStart) {

		//		TheEnd.DirectionInput(SourceInstance, ComingFrom);
		//	} else if (ComingFrom == TheEnd) {
		//		TheStart.DirectionInput(SourceInstance, ComingFrom);
		//	}
		//}
		//public void PassOnFlushSupplyAndUp(IntrinsicElectronicData ComingFrom)
		//{
		//	if (ComingFrom == TheStart) {

		//		TheEnd.FlushSupplyAndUp(SourceInstance);
		//	} else if (ComingFrom == TheEnd) {
		//		TheStart.FlushSupplyAndUp(SourceInstance);
		//	}
		//}

		//public void PassOnRemoveSupply(ElectricalOIinheritance ComingFrom,GameObject SourceInstance = null)
		//{
		//	if (ComingFrom == TheStart) {

		//		TheEnd.RemoveSupply(SourceInstance);
		//	} else if (ComingFrom == TheEnd) {
		//		TheStart.RemoveSupply(SourceInstance);
		//	}
		//	for (int i = 0; i<Covering.Count; i++)
		//	{
		//		Covering[i].Data.ActualVoltage = 0;
		//		Covering[i].Data.CurrentInWire = 0;
		//		Covering[i].Data.EstimatedResistance = 0;
		//	}
		//}


		//public void Kill()
		//{
		//	foreach (var Cable in Covering)
		//	{
		//		var Wire = Cable.Present as WireConnect;
		//		Wire.RelatedLine = null;           
		//	}
		//}

		//public void FlushConnectionAndUp(IntrinsicElectronicData ComingFrom)
		//{
		//	if (ComingFrom == TheStart)
		//	{
		//		TheEnd.Present.FlushConnectionAndUp();
		//	}
		//	else if (ComingFrom == TheEnd)
		//	{
		//		TheStart.Present.FlushConnectionAndUp();
		//	}

		//}

		//public void UpdateCoveringCable()
		//{
		//	foreach (var Cable in Covering)
		//	{
		//		Cable.Present.Data.ActualVoltage = TheStart.Present.Data.ActualVoltage;
		//		Cable.Present.Data.CurrentInWire = TheStart.Present.Data.CurrentInWire;
		//		Cable.Present.Data.EstimatedResistance = TheStart.Present.Data.EstimatedResistance;
		//	}

		//}
	}
}
