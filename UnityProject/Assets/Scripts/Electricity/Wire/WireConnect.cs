using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UnityEngine.Networking;
using UnityEngine;

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
		private bool connected = false;
		public int currentTick = 0;

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
					var conns = matrix.GetElectrictyConnections(pos);

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

		//Feed electricity into this wire:
		public void ElectricityInput(int tick){ //TODO A struct that can be passed between connections for Voltage, current etc
			
			//For testing (shows the yellow sphere gizmo that shows it is connected)
			connected = true;
			ElectricityOutput(tick);
		}

		//Output electricity to this next wire/object
		public void ElectricityOutput(int tick){
			if(currentTick == tick){
				//No need to process a tick twice
				return;
			}
			currentTick = tick;
			for (int i = 0; i < connections.Count; i++){
				connections[i].ElectricityInput(tick);
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
			ElectricityOutput(currentTick + 1);
		}
	}
}
