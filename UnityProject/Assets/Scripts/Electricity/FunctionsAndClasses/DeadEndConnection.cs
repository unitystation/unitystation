using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadEndConnection : ElectricalOIinheritance { //Used for formatting in the electrical system
	
	public override void FindPossibleConnections(){}

	public override void ElectricityInput(WrapCurrent Current, ElectricalOIinheritance SourceInstance,  ElectricalOIinheritance ComingFrom , ElectricalDirectionStep Path){}

	public override	void ElectricityOutput(WrapCurrent Current, ElectricalOIinheritance SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalDirectionStep Path){}


	public override	void ResistanceInput(ResistanceWrap Resistance,
										ElectricalOIinheritance SourceInstance,
										IntrinsicElectronicData ComingFrom,
										List<ElectricalDirectionStep> NetworkPath){}

	public override void ResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance, List<ElectricalDirectionStep> Directions){}

	public override void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn  = null){}

	public override	void DirectionOutput(GameObject SourceInstance){}

	public override	void FlushConnectionAndUp (){}


	public override	void FlushResistanceAndUp ( ElectricalOIinheritance SourceInstance = null ){}
	public override	void FlushSupplyAndUp ( ElectricalOIinheritance SourceInstance = null ){}

	public override void RemoveSupply(ElectricalDirectionStep Path, ElectricalOIinheritance SourceInstance = null) { }
	public override	void SetConnPoints(Connection WireEndA, Connection WireEndB){}

	public override	ConnPoint GetConnPoints(){return(new ConnPoint());}

	public override	GameObject GameObject(){return(null);}
}
//shhhh Ignore the new Mono behaviour warning