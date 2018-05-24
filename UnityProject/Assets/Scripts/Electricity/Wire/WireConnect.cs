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

		private RegisterItem registerTile;
		private Matrix matrix => registerTile.Matrix;
		private bool connected = false;

		void Start(){
			FindPossibleConnections();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			registerTile = gameObject.GetComponent<RegisterItem>();
		}

		void FindPossibleConnections(){
			possibleConns.Clear();
			Vector2 searchVec = transform.localPosition;
			searchVec.x -= 1;
			searchVec.y -= 1;
			for (int x = 0; x < 2; x++){
				for (int y = 0; y < 2; y++){
					Vector3Int pos = new Vector3Int((int)searchVec.x + x,
											  (int)searchVec.y + y, 0);
					var conns = matrix.GetElectrictyConnections(pos);
					foreach(IElectricityIO io in conns){
						Debug.Log("Found Electrical connection: " + pos);
						possibleConns.Add(io);
						//Test connection:
						io.ElectricityInput();
						//TODO Check if InputPosition and OutputPosition connect with this wire
					}

				}
			}	
		}

		void OnDrawGizmos(){
			if (connected) {
				Gizmos.color = Color.yellow;
				Gizmos.DrawSphere(transform.position, 0.1f);
			}
		}

		public int InputPosition(){
			return wire.DirectionStart;
		}

		public int OutputPosition(){
			return wire.DirectionEnd;
		}

		//Feed electricity into this wire:
		public void ElectricityInput(){

			//For testing:
			connected = true;
		}

		//Output electricity to this next wire/object
		public void ElectricityOutput(){
			
		}
	}
}
