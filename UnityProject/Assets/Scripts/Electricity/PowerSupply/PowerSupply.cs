using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Events;
using Events;

namespace Electricity
{
	public class PowerSupply : NetworkBehaviour, IElectricityIO
	{
		//A list of all devices connected to the circuit:
		public List<PoweredDevice> connectedDevices = new List<PoweredDevice>();

		//A list of all power generators connect to the circuit:
		public List<PowerGenerator> powerGenerators = new List<PowerGenerator>();

		//All power suppliers on this circuit:
		public List<PowerSupply> allSuppliers = new List<PowerSupply>();

		[Header("0 = conn to wire on same tile")]
		public int connPointA = 0;
		public int connPointB = -1;

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

		public void TurnOnSupply(float voltage, float current){
			
			//Tell the electrical network to check all of their connections:
			EventManager.Broadcast(EVENT.PowerNetSelfCheck);

			//Test //TODO calculate these values and implement a charge variable
			Voltage = voltage;
			Current = current;

			supplyElectricity = true;
		}

		public void TurnOffSupply(){
			supplyElectricity = false;
			Electricity supply = new Electricity();
			supply.voltage = 0f;
			supply.current = 0f;
			supply.suppliers = allSuppliers.ToArray();
			currentTick++;
			ElectricityOutput(currentTick, supply);
			OnCircuitChange.Invoke();
		}

		void Update(){
			if(supplyElectricity && connected){
				tickCount += Time.deltaTime;
				if(tickCount > tickRate){
					tickCount = 0f;
					currentTick++;
					//Supply the electricity
					Electricity supply = new Electricity();
					supply.voltage = Voltage;
					supply.current = Current;
					//TODO Make sure the actual power supply that is sending the struct is at index 0
					//If there are multiple suppliers on the network then they join together and act as one
					//with the supplier with the most charge and latest tick rate taking charge
					supply.suppliers = allSuppliers.ToArray();
					ElectricityOutput(currentTick, supply);
				}
			}
		}

		public void ElectricityInput(int tick, Electricity electricity)
		{			
			if(tick > currentTick){
				currentTick = tick;
				for (int i = 0; i < electricity.suppliers.Length; i++){
					//if any of the suppliers is not in the allSuppliers list then add it:
					if(!allSuppliers.Contains(electricity.suppliers[i])){
						allSuppliers.Add(electricity.suppliers[i]);
					}
				}
			}
			//Do not pass on electricty
			//TODO allow power generators to supply the charge of this Supplier
		}

		public void ElectricityOutput(int tick, Electricity electricity)
		{
			//Feed electrcity supply into the connected wire
			if (connectedWire != null) {
				connectedWire.ElectricityInput(tick, electricity);
			}
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
