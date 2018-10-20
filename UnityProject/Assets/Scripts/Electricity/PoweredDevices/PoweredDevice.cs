using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Collections.Generic;

	public class PoweredDevice : NetworkBehaviour, IElectricityIO
	{
		[Header("0 = conn to wire on same tile")]
		public int connPointA = 0;
		public int connPointB = -1;

		//The wire that is connected to this supply
		private IElectricityIO connectedWire;

		public RegisterObject registerTile;
		private Matrix matrix => registerTile.Matrix;
		public bool connected = false;
		public int currentTick;
		public float tickRate = 1f; //currently set to update every second
		private float tickCount = 0f;

		public List<IElectricityIO> connections {get; set;}
		public PowerSupply supplySource; //Where is the voltage coming from
		public Dictionary<int,Dictionary<IElectricityIO,float>> CurrentComingFrom {get; set;}
		public Dictionary<int,Dictionary<IElectricityIO,float>> ResistanceTosource {get; set;}
		public Dictionary<int,HashSet<IElectricityIO>> Downstream {get; set;}
		public Dictionary<int,HashSet<IElectricityIO>> Upstream {get; set;}
		public float ActualCurrent {get; set;}
		public PowerTypeCategory Categorytype { get; set; }
		public HashSet<PowerTypeCategory> CanConnectTo {get; set;}
		public int FirstPresent {get; set;}
		public int FirstPresentInspector = 0;
		public float ActualCurrentChargeInWire {get; set;}
		//For unity editor
		public int DownstreamCount;
		public int UpstreamCount;
		public float VisibleResistance;
		public float EditorActualCurrentChargeInWire;

		public float PassedDownResistance;

		public Electricity suppliedElectricity;

		//If the supply changes send an event to any action that has subscribed
		public UnityEvent OnSupplyChange;

		public void OnCircuitChanged(){
			SelfCheckState();
		}
		private void SelfCheckState(){
			Upstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			Downstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			ResistanceTosource = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
			//After the wire is discharged, wait 1f to see if it has charged again
			Invoke("CheckState", 1f);
		}
		void Awake(){
			Upstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			Downstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			ResistanceTosource = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
			CurrentComingFrom = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
			connections = new List<IElectricityIO> ();
		}
		private void OnEnable()
		{
			if (OnSupplyChange == null) {
				OnSupplyChange = new UnityEvent();
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			StartCoroutine(WaitForLoad());
		}

		IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(2f);
			FindPossibleConnections();
		}
		[ContextMenu("FindConnections")]
		void FindPossibleConnections(){
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
		
	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom){
		ElectricityFunctions.DirectionInput (tick, SourceInstance,ComingFrom, this);
		FirstPresentInspector = FirstPresent;
	} 
	public void DirectionOutput(int tick, GameObject SourceInstance) {
		//ElectricityFunctions.DirectionOutput (tick, SourceInstance, this);
		int SourceInstanceID = SourceInstance.GetInstanceID();
		DownstreamCount = Downstream [SourceInstanceID].Count;
		UpstreamCount = Upstream [SourceInstanceID].Count;
		PowerSupply SourceInstancPowerSupply = SourceInstance.GetComponent<PowerSupply> ();
		if (SourceInstancPowerSupply){
			SourceInstancPowerSupply.connectedDevices.Add (this);
		}
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}


	public void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){
		ElectricityFunctions.ResistanceInput (tick, Resistance, SourceInstance, ComingFrom, this);
	}

	//Output electricity to this next wire/object

	public void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance){
		PowerSupply SourceInstancPowerSupply = SourceInstance.GetComponent<PowerSupply> ();
		if (SourceInstancPowerSupply){
			if (SourceInstancPowerSupply.connectedDevices.Contains (this)) {
				Resistance = PassedDownResistance;
			}
		}
		VisibleResistance = Resistance; 
		ElectricityFunctions.ResistancyOutput(tick, Resistance, SourceInstance, this);
	}
	public void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){ 
		ElectricityFunctions.ElectricityInput(tick, Current, SourceInstance,  ComingFrom,this);

	}


	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance){
		EditorActualCurrentChargeInWire = ActualCurrentChargeInWire;
		//ElectricityFunctions.ElectricityOutput(tick,Current,SourceInstance,this);
	}
	public void StateImHere(){
		Logger.Log ("WOW YEAH!!!" + this.gameObject.ToString (), Category.Electrical);
	}
		public GameObject GameObject()
		{
			return gameObject;
		}

		public ConnPoint GetConnPoints()
		{
			ConnPoint points = new ConnPoint();
			points.pointA = connPointA;
			points.pointB = connPointB;
			return points;
		}
	}

