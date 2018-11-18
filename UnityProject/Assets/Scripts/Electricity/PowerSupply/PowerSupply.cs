using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Events;

public class PowerSupply : NetworkBehaviour, IElectricityIO , IProvidePower
{
	//A list of all devices connected to the circuit:
	public IElectricalNeedUpdate RelatedUpdateDevice {get; set;}

	[Header("0 = conn to wire on same tile")]
	public int DirectionStart;
	public int DirectionEnd;

	public HashSet<IElectricityIO> ResistanceToConnectedDevices {get; set;} = new HashSet<IElectricityIO>();
	public HashSet<IElectricityIO> connectedDevices {get; set;} = new HashSet<IElectricityIO>();
	public List<IElectricityIO> connections {get; set;} = new List<IElectricityIO> ();
	public Dictionary<int,Dictionary<IElectricityIO,float>> CurrentGoingTo{get; set;} = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
	public Dictionary<int,Dictionary<IElectricityIO,float>> CurrentComingFrom {get; set;} = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
	public Dictionary<int,Dictionary<IElectricityIO,float>> ResistanceComingFrom {get; set;} = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
	public Dictionary<int,Dictionary<IElectricityIO,float>> ResistanceGoingTo {get; set;} = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
	public Dictionary<int,float> SourceVoltages {get; set;}  = new Dictionary<int, float> ();
	public Dictionary<int,HashSet<IElectricityIO>> Downstream {get; set;} = new Dictionary<int, HashSet<IElectricityIO>> ();
	public Dictionary<int,HashSet<IElectricityIO>> Upstream {get; set;} = new Dictionary<int, HashSet<IElectricityIO>> ();
	public float ActualCurrent {get; set;}
	public PowerTypeCategory Categorytype { get; set; }
	public HashSet<PowerTypeCategory> CanConnectTo {get; set;}
	public int FirstPresent {get; set;} = new int();
	public int FirstPresentInspector = 0;
	public Electricity ActualCurrentChargeInWire {get; set;} = new Electricity();
	//For unity editor
	public float UpstreamCount {get; set;} = new float();
	public float DownstreamCount {get; set;} = new float();
	public float CurrentInWire  {get; set;} = new float();
	public float ActualVoltage {get; set;} = new float();
	public float EstimatedResistance {get; set;} = new float();

	public List<IElectricityIO> DirectionWorkOnNextList  {get; set;} = new List<IElectricityIO> ();

	public bool CanProvideResistance {get; set;} = false;
	public float PassedDownResistance {get; set;}

	public RegisterObject registerTile;
	private Matrix matrix => registerTile.Matrix;
	public bool connected = false;
	public bool supplyElectricity; //Provide electricity to the circuit or not
	public float SupplyingCurrent;
	//If the circuit changes send an event to all the elements that make up the circuit



	//Objects this wire is connected to

	public void FindPossibleConnections(){
		connections.Clear();
		connections = ElectricityFunctions.FindPossibleConnections(
			transform.localPosition,
			matrix,
			CanConnectTo,
			GetConnPoints()
		);
		if (connections.Count > 0){
			connected =  true;
		}

	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		//Not working for some reason:
		//registerTile = gameObject.GetComponent<RegisterItem>();
		StartCoroutine(WaitForLoad());
	}

	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		FindPossibleConnections();
	}

	public void TurnOnSupply(){

	}

	public void TurnOffSupply(){
		RemoveSupply (this.GameObject());
		//OnCircuitChange.Invoke();
	}

	public void PowerUpdateStructureChange(){
		//Logger.Log (connectedDevices.Count.ToString());
		FlushConnectionAndUp ();
		//CircuitSearchLoop ();

	}
	public void PowerUpdateStructureChangeReact(){
		CircuitSearchLoop ();
	}
	public void PowerUpdateResistanceChange(){
		//CircuitSearchLoop ();
		FlushResistanceAndUp (this.gameObject);
		//Logger.Log ("eyyp");
		if (connectedDevices.Count > 0) {
			//Logger.Log ("connectedDevices");
			foreach (IElectricityIO ConnectedDevice in connectedDevices) {
				//Logger.Log (ConnectedDevice.ToString () + "yea ConnectedDevice");
				//Logger.Log (this.gameObject.GetInstanceID().ToString() + " < Sending");
				ConnectedDevice.ResistanceInput (ElectricalSynchronisation.currentTick, 1.11111111f, this.gameObject, null);

			}
		}
	}
	public void PowerUpdateCurrentChange (){
		//Logger.Log (connections.Count.ToString ());
		FlushSupplyAndUp (this.gameObject); //Make a step one for removing from network vs  Removing the Pacific once data

		if (connectedDevices.Count > 0) {
			//Logger.Log ("connectedDevices");
			//int InstanceID = this.gameObject.GetInstanceID ();
			//float Resistance = ElectricityFunctions.WorkOutResistance (ResistanceComingFrom [InstanceID]);
			//Logger.Log (Resistance.ToString () + " Received resistance", Category.Electrical);
			//float Voltage = SupplyingCurrent * Resistance;
			if (SupplyingCurrent != 0) {
				ElectricityOutput (ElectricalSynchronisation.currentTick, SupplyingCurrent, this.gameObject);

			}

		}
	}

	public void PowerNetworkUpdate (){

	}


			

	public void CircuitSearchLoop(){
		DirectionOutput (ElectricalSynchronisation.currentTick, this.gameObject);
		while (DirectionWorkOnNextList.Count > 0) {
			List<IElectricityIO> IterateDirectionWorkOnNextList = new List<IElectricityIO> (DirectionWorkOnNextList);
			DirectionWorkOnNextList = new List<IElectricityIO> ();
			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 

				//Logger.Log (IterateDirectionWorkOnNextList [i].ToString ());
				IterateDirectionWorkOnNextList [i].DirectionOutput (ElectricalSynchronisation.currentTick, this.gameObject);
			}
		}
	}

	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO PassOn  = null){
		ElectricityFunctions.DirectionInput (tick, SourceInstance,ComingFrom, this);
		FirstPresentInspector = FirstPresent;
	} 
	public void DirectionOutput(int tick, GameObject SourceInstance) {
		ElectricityFunctions.DirectionOutput (tick, SourceInstance, this);
		int SourceInstanceID = SourceInstance.GetInstanceID();
		DownstreamCount = Downstream [SourceInstanceID].Count;
		UpstreamCount = Upstream [SourceInstanceID].Count;


		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}


	public void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){
		ElectricityFunctions.ResistanceInput (tick, Resistance, SourceInstance, ComingFrom, this);
	}

	//Output electricity to this next wire/object

	public void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance){
		//Logger.Log (SourceInstance.GetInstanceID().ToString() + " < Receive | is > " + this.gameObject.GetInstanceID().ToString() );
		if (!(SourceInstance == this.gameObject)) {
			ElectricityFunctions.ResistancyOutput (tick, Resistance, SourceInstance, this);
		}
		//VisibleResistance = Resistance; 
	}
	public void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){ 
		ElectricityFunctions.ElectricityInput(tick, Current, SourceInstance,  ComingFrom,this);

	}
	public void SetConnPoints(int DirectionEndin, int DirectionStartin){

	}

	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance){
		ElectricityFunctions.ElectricityOutput(tick,Current,SourceInstance,this);
		ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		CurrentInWire = ActualCurrentChargeInWire.Current;
		ActualVoltage = ActualCurrentChargeInWire.Voltage;
		EstimatedResistance = ActualCurrentChargeInWire.EstimatedResistant;
		//EstimatedResistance = ActualCurrentChargeInWire.Voltage;
	}
	public GameObject GameObject()
	{
		return gameObject;
	}

	public ConnPoint GetConnPoints()
	{
		ConnPoint points = new ConnPoint();
		points.pointA = DirectionStart;
		points.pointB = DirectionEnd;
		return points;
	}

	public void FlushConnectionAndUp ( ){
		ElectricityFunctions.PowerSupplies.FlushConnectionAndUp (this);
	}
	public void FlushResistanceAndUp (  GameObject SourceInstance = null  ){
		ElectricityFunctions.PowerSupplies.FlushResistanceAndUp (this, SourceInstance);
	}
	public void FlushSupplyAndUp ( GameObject SourceInstance = null ){
		ElectricityFunctions.PowerSupplies.FlushSupplyAndUp (this, SourceInstance);
	}
	public void RemoveSupply( GameObject SourceInstance = null){
		ElectricityFunctions.PowerSupplies.RemoveSupply (this, SourceInstance);
	}

	[ContextMethod("Details","Magnifying_glass")]
	public void ShowDetails(){
		Logger.Log("connections " + (connections.Count.ToString()), Category.Electrical);
		Logger.Log ("ID " + (this.GetInstanceID ()), Category.Electrical);
		Logger.Log ("Type " + (Categorytype.ToString()), Category.Electrical);
		Logger.Log ("Can connect to " + (string.Join(",", CanConnectTo)), Category.Electrical);
		Logger.Log("UpstreamCount " + (UpstreamCount.ToString()), Category.Electrical);
		Logger.Log("DownstreamCount " + (DownstreamCount.ToString()), Category.Electrical);
		Logger.Log("CurrentInWire " + (CurrentInWire.ToString()), Category.Electrical);
		Logger.Log("ActualVoltage " + (ActualVoltage.ToString()), Category.Electrical);
		Logger.Log("EstimatedResistance " + (EstimatedResistance.ToString()), Category.Electrical);
	}

}

