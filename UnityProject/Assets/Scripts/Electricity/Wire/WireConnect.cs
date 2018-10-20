using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;


public class WireConnect : NetworkBehaviour, IElectricityIO
	{
		public StructurePowerWire wire;

		//Objects in reach of this wire:
		private List<IElectricityIO> possibleConns = new List<IElectricityIO>();

		//Objects this wire is connected to
		

		private RegisterItem registerTile;
		private Matrix matrix => registerTile.Matrix;

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
		//public Dictionary<int,float> ResistanceTosource;
		

		private bool connected = false;
		public int currentTick = 0;

		private void OnEnable()
		{
			EventManager.AddHandler(EVENT.PowerNetSelfCheck, FindPossibleConnections);
			if(supplySource != null){
				supplySource.OnCircuitChange.AddListener(OnCircuitChanged);
			}
		}
		[ContextMethod("Details","Magnifying_glass")]
		public void ShowDetails(){
			Logger.Log("connections " + (connections.Count.ToString()), Category.Electrical);
			Logger.Log("possibleConns " + (possibleConns.Count.ToString()), Category.Electrical);
			Logger.Log ("ID " + (this.GetInstanceID ()), Category.Electrical);
			Logger.Log ("Type " + (Categorytype.ToString()), Category.Electrical);
			Logger.Log ("Can connect to " + (string.Join(",", CanConnectTo)), Category.Electrical);
		}
		private void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.PowerNetSelfCheck, FindPossibleConnections);
			if (supplySource != null) {
				supplySource.OnCircuitChange.RemoveListener(OnCircuitChanged);
			}
		}
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

		[ContextMenu("FindConnections")]
		void FindPossibleConnections(){
			connections.Clear();
			connections = ElectricityFunctions.FindPossibleConnections(
				transform.localPosition,
				matrix,
				CanConnectTo,
				GetConnPoints()
			);
		}

		void OnDrawGizmos(){
			if (connected) {
				Gizmos.color = Color.yellow;
				Gizmos.DrawSphere(transform.position, 0.1f);
			}
		}

		public ConnPoint GetConnPoints(){
			ConnPoint conns = new ConnPoint();
			conns.pointA = wire.DirectionStart;
			conns.pointB = wire.DirectionEnd;
			return conns;
		}

		public int InputPosition(){
			return wire.DirectionStart;
		}

		public int OutputPosition(){
			return wire.DirectionEnd;
		}

		public GameObject GameObject(){
			return gameObject;
		}

		//Do things when the circuit breaks etc
		public void OnCircuitChanged(){
			//TODO recheck connectivity and drain voltage from wire
			//TODO Remove wire from PowerSupply event if it is no longer connected to it
			SelfCheckState();
		}
	void Awake(){
		Upstream = new Dictionary<int, HashSet<IElectricityIO>> ();
		Downstream = new Dictionary<int, HashSet<IElectricityIO>> ();
		ResistanceTosource = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
		CurrentComingFrom = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
		connections = new List<IElectricityIO> ();
	}

		//Start a monitor of its own state (triggered by a circuit change event)
		private void SelfCheckState(){
			//clear the charge in the wire:

			ResistanceTosource = new Dictionary<int, Dictionary<IElectricityIO, float>>();
			Upstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			Downstream = new Dictionary<int, HashSet<IElectricityIO>> ();
			CurrentComingFrom = new Dictionary<int, Dictionary<IElectricityIO, float>> ();
			//After the wire is discharged, wait 1f to see if it has charged again
			Invoke("CheckState", 1f);
		}

		//See if there is charge in the wire:
		private void CheckState(){
//			if(currentChargeInWire.suppliers.Length == 0){
//				if(supplySource != null){
//					//Wire is broken, leave wire discharged and remove the event
//					supplySource.OnCircuitChange.RemoveListener(OnCircuitChanged);
//					connected = false;
//					supplySource = null;
//				}
//			} //else its all good, leave as is
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
		VisibleResistance = Resistance; 
		ElectricityFunctions.ResistancyOutput(tick, Resistance, SourceInstance, this);
	}
	public void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){ 
		ElectricityFunctions.ElectricityInput (tick, Current, SourceInstance, ComingFrom, this);

	}


	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance){
		EditorActualCurrentChargeInWire = ActualCurrentChargeInWire;
		//Logger.Log (EditorActualCurrentChargeInWire.ToString () + " How much current", Category.Electrical);
		ElectricityFunctions.ElectricityOutput(tick,Current,SourceInstance,this);
	}
		

		[ContextMenu("PrintAllLists")]
		public void DebugPrintLists(){
			
			foreach(IElectricityIO io in possibleConns){
				Logger.Log($" Possilbe conn at {io.GameObject().transform.localPosition}",Category.Power);
			}

			foreach (IElectricityIO io in connections) {
				Logger.Log($" Connected to something at {io.GameObject().transform.localPosition}",Category.Power);
			}

			Logger.Log($"The connection points are: {wire.DirectionStart} and {wire.DirectionEnd}",Category.Power);
		}

		[ContextMenu("GenerateTestCurrent")]
		public void GenerateTestElectricity(){
//			connected = true;
//			Electricity newElec = new Electricity();
//			ElectricityOutput(currentTick + 1, newElec);
		}
	}

