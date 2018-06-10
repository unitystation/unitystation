using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UnityEngine.Networking;
using UnityEngine;

namespace Electricity
{
	public class PowerSupply : NetworkBehaviour, IElectricityIO
	{
		[Header("0 = conn to wire on same tile")]
		public int connPointA = 0;
		public int connPointB = -1;

		//The wire that is connected to this supply
		private IElectricityIO connectedWire;

		public RegisterObject registerTile;
		private Matrix matrix => registerTile.Matrix;
		public bool connected = false;

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

		void FindPossibleConnections()
		{
			var conns = matrix.GetElectrictyConnections(Vector3Int.RoundToInt(transform.localPosition));

			foreach (IElectricityIO io in conns) {

				//Check if InputPosition and OutputPosition connect with this wire
				if (ConnectionMap.IsConnectedToTile(GetConnPoints(), AdjDir.Overlap, io.GetConnPoints())) {
					connectedWire = io;
					connected = true;
					break;
				}
			}
		}

		public void ElectricityInput(int currentTick, Electricity electricity)
		{

		}

		public void ElectricityOutput(int currentTick, Electricity electricity)
		{

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
}
