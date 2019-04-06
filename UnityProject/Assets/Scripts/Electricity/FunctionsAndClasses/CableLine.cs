using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableLine { //Meant to be an intermediary for long stretches of cable so as to Reduce processing time on Long cables 
	public GameObject InitialGenerator;
	public ElectricalOIinheritance TheStart;
	public ElectricalOIinheritance TheEnd;
	public List<ElectricalOIinheritance> Covering = new List<ElectricalOIinheritance>();


	public void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn  = null){
		JumpToOtherEnd (SourceInstance, ComingFrom);
	}

	public void JumpToOtherEnd(GameObject SourceInstance,ElectricalOIinheritance ComingFrom){
		if (ComingFrom == TheStart) {

			TheEnd.DirectionInput(SourceInstance, ComingFrom);
		} else if (ComingFrom == TheEnd) {
			TheStart.DirectionInput(SourceInstance, ComingFrom);
		}
	}
	public void PassOnFlushSupplyAndUp(ElectricalOIinheritance ComingFrom,GameObject SourceInstance = null)
	{
		if (ComingFrom == TheStart) {

			TheEnd.FlushSupplyAndUp(SourceInstance);
		} else if (ComingFrom == TheEnd) {
			TheStart.FlushSupplyAndUp(SourceInstance);
		}
	}

	public void PassOnRemoveSupply(ElectricalOIinheritance ComingFrom,GameObject SourceInstance = null)
	{
		if (ComingFrom == TheStart) {

			TheEnd.RemoveSupply(SourceInstance);
		} else if (ComingFrom == TheEnd) {
			TheStart.RemoveSupply(SourceInstance);
		}
		for (int i = 0; i<Covering.Count; i++)
		{
			Covering[i].Data.ActualVoltage = 0;
			Covering[i].Data.CurrentInWire = 0;
			Covering[i].Data.EstimatedResistance = 0;
		}
	}

	public void UpdateCoveringCable()
	{
		for (int i = 0; i< Covering.Count; i++)
		{
			Covering[i].Data.ActualVoltage = TheStart.Data.ActualVoltage;
			Covering[i].Data.CurrentInWire = TheStart.Data.CurrentInWire;
			Covering[i].Data.EstimatedResistance = TheStart.Data.EstimatedResistance;
		}
	}

}
