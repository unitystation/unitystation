using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Events;


public class PowerSupply : NetworkBehaviour, IElectricityIO
	{
		//A list of all devices connected to the circuit:
		public HashSet<PoweredDevice> connectedDevices = new HashSet<PoweredDevice>();

		//A list of all power generators connect to the circuit:
		public List<PowerGenerator> powerGenerators = new List<PowerGenerator>();

		//All power suppliers on this circuit:
		public List<PowerSupply> allSuppliers = new List<PowerSupply>();

		[Header("0 = conn to wire on same tile")]
		public int connPointA = 0;
		public int connPointB = -1;

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
	public float SupplyingCurrent;
	public float EditorActualCurrentChargeInWire;


		//Used to work out the way current and voltage will we work out


		//The wire that is connected to this supply
		private IElectricityIO connectedWire;

		public RegisterObject registerTile;
		private Matrix matrix => registerTile.Matrix;
		public bool connected = false;
		public bool supplyElectricity; //Provide electricity to the circuit or not
		public int currentTick;
		public float tickRate = 1f; //currently set to update every second
		private float tickCount = 0f;

		//If the circuit changes send an event to all the elements that make up the circuit
		public UnityEvent OnCircuitChange;

		//TEST ELECTRICITY PROPERTIES (TESTING WITH UNLIMITED CHARGE)
		public float Voltage;
		public float Current;

		//Objects this wire is connected to

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
		void Awake(){
			Upstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			Downstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			ResistanceTosource = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
			CurrentComingFrom = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
			connections = new List<IElectricityIO> ();
			
		}
		[ContextMethod("Details","Magnifying_glass")]
		public void ShowDetails(){
			Logger.Log("connections " + (connections.Count.ToString()), Category.Electrical);
			Logger.Log ("ID " + (this.GetInstanceID ()), Category.Electrical);
			Logger.Log ("Type " + (Categorytype.ToString()), Category.Electrical);
			Logger.Log ("Can connect to " + (string.Join(",", CanConnectTo)), Category.Electrical);
		}

		private void OnEnable()
		{
			EventManager.AddHandler(EVENT.PowerNetSelfCheck, FindPossibleConnections);
			if (OnCircuitChange == null) {
				OnCircuitChange = new UnityEvent();
			}
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.PowerNetSelfCheck, FindPossibleConnections);
		}
		public override void OnStartClient()
		{
			base.OnStartClient();
			//Not working for some reason:
			//registerTile = gameObject.GetComponent<RegisterItem>();
			StartCoroutine(WaitForLoad());
			allSuppliers.Add(this);
		}

		IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(2f);
			FindPossibleConnections();
		}
		
		public void TurnOnSupply(float current){
		SupplyingCurrent = current;
			//Tell the electrical network to check all of their connections:
			//EventManager.Broadcast(EVENT.PowerNetSelfCheck);
			//Test //TODO calculate these values and implement a charge variable
			//Voltage = voltage;
			//Current = current;

			supplyElectricity = true;
		}

		public void TurnOffSupply(){
			supplyElectricity = false;
			Electricity supply = new Electricity();
			supply.voltage = 0f;
			supply.current = 0f;
//			supply.suppliers = allSuppliers.ToArray();
			currentTick++;
//			ElectricityOutput(currentTick, supply);
			OnCircuitChange.Invoke();
		}

		void Update(){
			if(supplyElectricity && connected){
				tickCount += Time.deltaTime;
				if(tickCount > tickRate){
					tickCount = 0f;
					currentTick++;
				DirectionInput (currentTick, this.gameObject, this.GetComponent<IElectricityIO>());
				if (connectedDevices.Count > 0) {
					foreach (IElectricityIO ConnectedDevice in connectedDevices) {
						ConnectedDevice.ResistanceInput (currentTick,1.11111111f, this.gameObject, ConnectedDevice);
					}
					int InstanceID = this.gameObject.GetInstanceID ();
					float Resistance = ElectricityFunctions.WorkOutResistance (ResistanceTosource[InstanceID]);
					Logger.Log (Resistance.ToString () + " Received resistance", Category.Electrical);
					float Voltage = SupplyingCurrent * Resistance;
					ElectricityInput (currentTick, SupplyingCurrent, this.gameObject, this);
				}
					//Supply the electricity
//					Electricity supply = new Electricity();
//					supply.voltage = Voltage;
//					supply.current = Current;
					//TODO Make sure the actual power supply that is sending the struct is at index 0
					//If there are multiple suppliers on the network then they join together and act as one
					//with the supplier with the most charge and latest tick rate taking charge
//					supply.suppliers = allSuppliers.ToArray();
//					ElectricityOutput(currentTick, supply);
				}
			}
		}

	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom){
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
		if (!(SourceInstance == this.gameObject)) {
			ElectricityFunctions.ResistancyOutput (tick, Resistance, SourceInstance, this);
		}
		VisibleResistance = Resistance; 
	}
	public void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){ 
		ElectricityFunctions.ElectricityInput(tick, Current, SourceInstance,  ComingFrom,this);

	}


	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance){
		EditorActualCurrentChargeInWire = ActualCurrentChargeInWire;
		ElectricityFunctions.ElectricityOutput(tick,Current,SourceInstance,this);
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

