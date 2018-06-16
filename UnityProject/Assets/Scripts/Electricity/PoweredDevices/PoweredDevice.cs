using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace Electricity
{
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

		public Electricity suppliedElectricity;

		//If the supply changes send an event to any action that has subscribed
		public UnityEvent OnSupplyChange;


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

		void FindPossibleConnections()
		{
			var conns = matrix.GetElectricalConnections(Vector3Int.RoundToInt(transform.localPosition));

			foreach (IElectricityIO io in conns) {

				//Check if InputPosition and OutputPosition connect with this wire
				if (ConnectionMap.IsConnectedToTile(GetConnPoints(), AdjDir.Overlap, io.GetConnPoints()) &&
					io.GameObject() != gameObject) {
					connectedWire = io;
					connected = true;
					break;
				}
			}
		}

		public void ElectricityInput(int tick, Electricity electricity)
		{
			if (tick > currentTick) {
				currentTick = tick;
				suppliedElectricity = electricity;
				//Send event that the supply has updated
				OnSupplyChange.Invoke();
			}
			//Do not pass on electricty
		}

		public void ElectricityOutput(int tick, Electricity electricity)
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
