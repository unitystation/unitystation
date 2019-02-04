using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadEndConnection : IElectricityIO { //Used for formatting in the electrical system
	
	public PowerTypeCategory Categorytype {get; set;} = PowerTypeCategory.DeadEndConnection;

	public ElectronicData Data {get; set;} = new ElectronicData();
	public IntrinsicElectronicData InData  {get; set;} = new IntrinsicElectronicData();
	public HashSet<IElectricityIO> connectedDevices {get; set;} 


	public void FindPossibleConnections(){}

	public void ElectricityInput(float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){}

	public 	void ElectricityOutput(float Current, GameObject SourceInstance){}


	public 	void ResistanceInput(float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){}

	public void ResistancyOutput(GameObject SourceInstance){}

	public void DirectionInput(GameObject SourceInstance, IElectricityIO ComingFrom, CableLine PassOn  = null){}

	public 	void DirectionOutput(GameObject SourceInstance){}

	public 	void FlushConnectionAndUp (){}


	public 	void FlushResistanceAndUp ( GameObject SourceInstance = null ){}
	public 	void FlushSupplyAndUp ( GameObject SourceInstance = null ){}

	public 	void RemoveSupply (GameObject SourceInstance = null){}
	public 	void SetConnPoints(int DirectionEnd, int DirectionStart){}

	public 	ConnPoint GetConnPoints(){return(new ConnPoint());}

	public 	GameObject GameObject(){return(null);}
}
