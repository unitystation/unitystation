using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Events;



public class WireConnect : NetworkBehaviour, IElectricityIO
{
	public int DirectionStart;
	public int DirectionEnd;

	public IElectricalNeedUpdate RelatedUpdateDevice {get; set;}


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

	public float SupplyingCurrent;

	public bool CanProvideResistance {get; set;} = false;
	public float PassedDownResistance {get; set;}

	public List<IElectricityIO> DirectionWorkOnNextList  {get; set;} = new List<IElectricityIO> ();



	public IElectricityIO CameFromMemory;

	public RegisterItem registerTile;
	private Matrix matrix => registerTile.Matrix;
	public bool connected = false;
	public bool supplyElectricity; //Provide electricity to the circuit or not


	public override void OnStartClient()
	{
		base.OnStartClient();
		registerTile = gameObject.GetComponent<RegisterItem>();
		StartCoroutine(WaitForLoad());
	}

	IEnumerator WaitForLoad(){
		yield return new WaitForSeconds(1f);
		FindPossibleConnections();
	}

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

	void OnDrawGizmos(){
		if (connected) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(transform.position, 0.1f);
		}
	}

	public ConnPoint GetConnPoints(){
		ConnPoint conns = new ConnPoint();
		conns.pointA = DirectionStart;
		conns.pointB = DirectionEnd;
		return conns;
	}

	public int InputPosition(){
		return DirectionStart;
	}

	public int OutputPosition(){
		return DirectionEnd;
	}

	public GameObject GameObject(){
		return gameObject;
	}


	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO PassOn  = null){
		//Logger.Log(SourceInstance.ToString() + " < SourceInstance " + ComingFrom.ToString() + " < ComingFrom " + this.name + " < this " );
		if (connections.Count > 2) {
			ElectricityFunctions.DirectionInput (tick, SourceInstance,ComingFrom, this);
			FirstPresentInspector = FirstPresent;
			
		} else {
			int SourceInstanceID = SourceInstance.GetInstanceID ();
			if (!(Upstream.ContainsKey (SourceInstanceID))) {
				Upstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
			}
			if (!(Downstream.ContainsKey (SourceInstanceID))) {
				Downstream [SourceInstanceID] = new HashSet<IElectricityIO> ();
			}
			if (FirstPresent == 0) {
				//Logger.Log ("to It's been claimed", Category.Electrical);
				FirstPresent = SourceInstanceID;
				//Thiswire.FirstPresentInspector = SourceInstanceID;
			}

			if (ComingFrom != null) {
				Upstream [SourceInstanceID].Add(ComingFrom);
			}
			CameFromMemory = PassOn;
			SourceInstance.GetComponent<IProvidePower> ().DirectionWorkOnNextList.Add (this);
		}


	} 
	public void DirectionOutput(int tick, GameObject SourceInstance) {
		int SourceInstanceID = SourceInstance.GetInstanceID();
//		foreach (IElectricityIO ConnectedTo in Downstream [SourceInstanceID]) {
//			Logger.Log (ConnectedTo.ToString () + "Special connection On wire" + this.name);
//		}
		//Logger.Log("to man");
		if (connections.Count > 2) {
			//Logger.Log ("Greater than 2");
			ElectricityFunctions.DirectionOutput (tick, SourceInstance, this);
			//int SourceInstanceID = SourceInstance.GetInstanceID();
			DownstreamCount = Downstream [SourceInstanceID].Count;
			UpstreamCount = Upstream [SourceInstanceID].Count;
		} else {
			//int SourceInstanceID = SourceInstance.GetInstanceID ();
			//Logger.Log ("not than 2 " + connections.Count.ToString());

			for (int i = 0; i < connections.Count; i++) {
				if (!(Upstream [SourceInstanceID].Contains (connections [i])) && (!(this == connections [i]))) {

					if (!(Downstream[SourceInstanceID].Contains (connections [i]))) {
						Downstream [SourceInstanceID].Add (connections [i]);

						connections [i].DirectionInput (tick, SourceInstance,this ,CameFromMemory);
					}


				} 
			}


		}

		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}


	public void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){
		//if (Time.time > 20) {
			//Logger.Log ("heLP!!!");
		//} else {
		ElectricityFunctions.ResistanceInput (tick, Resistance, SourceInstance, ComingFrom, this);
		//}

	}

	//Output electricity to this next wire/object

	public void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance){
		//VisibleResistance = Resistance; 
		ElectricityFunctions.ResistancyOutput(tick, Resistance, SourceInstance, this);
	}
	public void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){ 
		ElectricityFunctions.ElectricityInput (tick, Current, SourceInstance, ComingFrom, this);

	}


	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance){
		//Logger.Log(Current.ToString() + "yoree");
		//Logger.Log (CurrentInWire.ToString () + " How much current", Category.Electrical);
		ElectricityFunctions.ElectricityOutput(tick,Current,SourceInstance,this);
		CurrentInWire = ActualCurrentChargeInWire.Current;
		ActualVoltage = ActualCurrentChargeInWire.Voltage;
		EstimatedResistance = ActualCurrentChargeInWire.EstimatedResistant;

	}
	public void SetConnPoints(int DirectionEndin, int DirectionStartin){
		DirectionEnd = DirectionEndin;
		DirectionStart = DirectionStartin;
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

