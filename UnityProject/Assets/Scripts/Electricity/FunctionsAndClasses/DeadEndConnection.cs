using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadEndConnection : ElectricalOIinheritance { //Used for formatting in the electrical system
	
	public override void FindPossibleConnections(){}

	public override void ElectricityInput(float Current, GameObject SourceInstance,  ElectricalOIinheritance ComingFrom){}

	public override	void ElectricityOutput(float Current, GameObject SourceInstance){}


	public override	void ResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom  ){}

	public override void ResistancyOutput(GameObject SourceInstance){}

	public override void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn  = null){}

	public override	void DirectionOutput(GameObject SourceInstance){}

	public override	void FlushConnectionAndUp (){}


	public override	void FlushResistanceAndUp ( GameObject SourceInstance = null ){}
	public override	void FlushSupplyAndUp ( GameObject SourceInstance = null ){}

	public override	void RemoveSupply (GameObject SourceInstance = null){}
	public override	void SetConnPoints(Connection WireEndA, Connection WireEndB){}

	public override	ConnPoint GetConnPoints(){return(new ConnPoint());}

	public override	GameObject GameObject(){return(null);}
}
//shhhh Ignore the new Mono behaviour warning