using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UnityEngine.Networking;
using UnityEngine;
using Events;

namespace Electricity
{
	public class WireConnect : NetworkBehaviour, IElectricityIO
	{
		public StructurePowerWire wire;

		//Objects in reach of this wire:
		private List<IElectricityIO> possibleConns = new List<IElectricityIO>();

		//Objects this wire is connected to
		private List<IElectricityIO> connections = new List<IElectricityIO>();

		private RegisterItem registerTile;
		private Matrix matrix => registerTile.Matrix;
		public PowerSupply supplySource; //Where is the voltage coming from
		public Electricity currentChargeInWire;

		private bool connected = false;
		public int currentTick = 0;

		private void OnEnable()
		{
			EventManager.AddHandler(EVENT.PowerNetSelfCheck, FindPossibleConnections);
			if(supplySource != null){
				supplySource.OnCircuitChange.AddListener(OnCircuitChanged);
			}
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
			possibleConns.Clear();
			connections.Clear();
			int progress = 0;
			Vector2 searchVec = transform.localPosition;
			searchVec.x -= 1;
			searchVec.y -= 1;
			for (int x = 0; x < 3; x++){
				for (int y = 0; y < 3; y++){
					Vector3Int pos = new Vector3Int((int)searchVec.x + x,
											  (int)searchVec.y + y, 0);
					var conns = matrix.GetElectricalConnections(pos);

					foreach(IElectricityIO io in conns){
						possibleConns.Add(io);

						//Check if InputPosition and OutputPosition connect with this wire
						if(ConnectionMap.IsConnectedToTile(GetConnPoints(), (AdjDir)progress, io.GetConnPoints())){
							connections.Add(io);
						} 
					}
					progress++;
				}
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

		//Start a monitor of its own state (triggered by a circuit change event)
		private void SelfCheckState(){
			//clear the charge in the wire:
			currentChargeInWire.voltage = 0f;
			currentChargeInWire.current = 0f;
			currentChargeInWire.suppliers = new PowerSupply[0];

			//After the wire is discharged, wait 1f to see if it has charged again
			Invoke("CheckState", 1f);
		}

		//See if there is charge in the wire:
		private void CheckState(){
			if(currentChargeInWire.suppliers.Length == 0){
				if(supplySource != null){
					//Wire is broken, leave wire discharged and remove the event
					supplySource.OnCircuitChange.RemoveListener(OnCircuitChanged);
					connected = false;
					supplySource = null;
				}
			} //else its all good, leave as is
		}

		//Feed electricity into this wire:
		public void ElectricityInput(int tick, Electricity electricity){ //TODO A struct that can be passed between connections for Voltage, current etc
			currentChargeInWire = electricity;

			//The supply found at index 0 is the actual source that has combined the resources of the other sources and
			//sent the actual electrcity struct. So reference that one for supplySource
			if(supplySource != electricity.suppliers[0]){
				supplySource = electricity.suppliers[0];
				supplySource.OnCircuitChange.AddListener(OnCircuitChanged);
			}
			//For testing (shows the yellow sphere gizmo that shows it is connected)
			if (electricity.voltage > 1f) {
				connected = true;
			}

			//Pass the charge on:
			ElectricityOutput(tick, currentChargeInWire);
		}

		//Output electricity to this next wire/object
		public void ElectricityOutput(int tick, Electricity electricity){
			if(currentTick == tick){
				//No need to process a tick twice
				return;
			}
			currentTick = tick;
			for (int i = 0; i < connections.Count; i++){
				connections[i].ElectricityInput(tick, electricity);
			}
		}

		[ContextMenu("PrintAllLists")]
		public void DebugPrintLists(){
			foreach(IElectricityIO io in possibleConns){
				Debug.Log($" Possilbe conn at {io.GameObject().transform.localPosition}");
			}

			foreach (IElectricityIO io in connections) {
				Debug.Log($" Connected to something at {io.GameObject().transform.localPosition}");
			}

			Debug.Log($"The connection points are: {wire.DirectionStart} and {wire.DirectionEnd}");
		}

		[ContextMenu("GenerateTestCurrent")]
		public void GenerateTestElectricity(){
			connected = true;
			Electricity newElec = new Electricity();
			ElectricityOutput(currentTick + 1, newElec);
		}
	}
}
