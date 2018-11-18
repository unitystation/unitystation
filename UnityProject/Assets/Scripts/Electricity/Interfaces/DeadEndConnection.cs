using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadEndConnection : IElectricityIO {
	
	public PowerTypeCategory Categorytype {get; set;} = PowerTypeCategory.DeadEndConnection;

	public HashSet<IElectricityIO> ResistanceToConnectedDevices {get; set;}
	public HashSet<IElectricityIO> connectedDevices {get; set;} 
	public HashSet<PowerTypeCategory> CanConnectTo {get; set;}
	public int FirstPresent {get; set;}
	public Dictionary<int,HashSet<IElectricityIO>> Downstream {get; set;}
	public Dictionary<int,HashSet<IElectricityIO>> Upstream {get; set;}
	public 	Dictionary<int,Dictionary<IElectricityIO,float>> ResistanceComingFrom {get; set;}
	public Dictionary<int,Dictionary<IElectricityIO,float>> ResistanceGoingTo {get; set;}
	public 	Dictionary<int,float> SourceVoltages {get; set;}
	public Dictionary<int,Dictionary<IElectricityIO,float>> CurrentGoingTo{get; set;}
	public 	Dictionary<int,Dictionary<IElectricityIO,float>> CurrentComingFrom {get; set;}
	public Electricity ActualCurrentChargeInWire {get; set;}
	public List<IElectricityIO> connections {get; set;}
	public 	bool CanProvideResistance {get; set;}
	public 	float PassedDownResistance {get; set;}

	public 	float UpstreamCount {get; set;}
	public float DownstreamCount {get; set;}
	public float CurrentInWire  {get; set;}
	public 	float ActualVoltage {get; set;}
	public 	float EstimatedResistance {get; set;}

	public void FindPossibleConnections(){}

	public void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){}

	public 	void ElectricityOutput(int tick, float Current, GameObject SourceInstance){}


	public 	void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){}

	public void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance){}

	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO PassOn  = null){}

	public 	void DirectionOutput(int tick, GameObject SourceInstance){}

	public 	void FlushConnectionAndUp (){}


	public 	void FlushResistanceAndUp ( GameObject SourceInstance = null ){}
	public 	void FlushSupplyAndUp ( GameObject SourceInstance = null ){}

	public 	void RemoveSupply (GameObject SourceInstance = null){}
	public 	void SetConnPoints(int DirectionEnd, int DirectionStart){}

	public 	ConnPoint GetConnPoints(){return(new ConnPoint());}

	public 	GameObject GameObject(){return(null);}
}
