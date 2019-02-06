using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableLine { //Meant to be an intermediary for long stretches of cable so as to Reduce processing time on Long cables 
	public GameObject InitialGenerator;
	public IElectricityIO TheStart;
	public IElectricityIO TheEnd;
	public List<IElectricityIO> Covering = new List<IElectricityIO>();


	public void DirectionInput(GameObject SourceInstance, IElectricityIO ComingFrom, CableLine PassOn  = null){
		JumpToOtherEnd (SourceInstance, ComingFrom);
	}

	public void JumpToOtherEnd(GameObject SourceInstance,IElectricityIO ComingFrom){
		if (ComingFrom == TheStart) {

			TheEnd.DirectionInput(SourceInstance, ComingFrom);
		} else if (ComingFrom == TheEnd) {
			TheStart.DirectionInput(SourceInstance, ComingFrom);
		}
	}
	public void PassOnFlushSupplyAndUp(IElectricityIO ComingFrom,GameObject SourceInstance = null)
	{
		if (ComingFrom == TheStart) {

			TheEnd.FlushSupplyAndUp(SourceInstance);
		} else if (ComingFrom == TheEnd) {
			TheStart.FlushSupplyAndUp(SourceInstance);
		}
	}

	public void PassOnRemoveSupply(IElectricityIO ComingFrom,GameObject SourceInstance = null)
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

	public void UpdateCoveringCable(IElectricityIO ComingFrom)
	{
		for (int i = 0; i< Covering.Count; i++)
		{
			Covering[i].Data.ActualVoltage = ComingFrom.Data.ActualVoltage;
			Covering[i].Data.CurrentInWire = ComingFrom.Data.CurrentInWire;
			Covering[i].Data.EstimatedResistance = ComingFrom.Data.EstimatedResistance;
		}	}

}
