using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ElectricalDataCleanup { //To clean out data on cables and machines
	public static void CleanConnectedDevices(ElectricalOIinheritance Thiswire){
		//Logger.Log ("Cleaning it out");
		foreach (KeyValuePair<ElectricalOIinheritance,HashSet<PowerTypeCategory>> IsConnectedTo in Thiswire.Data.ResistanceToConnectedDevices) {
			IsConnectedTo.Key.connectedDevices.Remove (Thiswire);
		}
		Thiswire.Data.ResistanceToConnectedDevices.Clear();
	}

	public static void CleanConnectedDevicesFromPower(ElectricalOIinheritance Thiswire){
		//Logger.Log ("Cleaning it out");
		foreach (ElectricalOIinheritance IsConnectedTo in Thiswire.connectedDevices) {
			IsConnectedTo.Data.ResistanceToConnectedDevices.Remove (Thiswire);
		}
		Thiswire.connectedDevices.Clear();
	}

	public static class PowerSupplies{
		public static void FlushConnectionAndUp (ElectricalOIinheritance Object){
			if (Object.Data.connections.Count > 0) {
				List<ElectricalOIinheritance> Backupconnections = Object.Data.connections;
				Object.Data.connections.Clear();
				foreach (ElectricalOIinheritance JumpTo in Backupconnections) {
					JumpTo.FlushConnectionAndUp ();
				}
				Object.Data.Upstream.Clear();
				Object.Data.Downstream.Clear();
				Object.Data.ResistanceComingFrom.Clear();
				Object.Data.ResistanceGoingTo.Clear();
				Object.Data.CurrentGoingTo.Clear();
				Object.Data.CurrentComingFrom.Clear();
				Object.Data.SourceVoltages = new Dictionary<int, float> ();
				Object.Data.CurrentInWire = new float ();
				Object.Data.ActualVoltage = new float ();
				Object.Data.ResistanceToConnectedDevices.Clear();
				Object.connectedDevices.Clear();
			}

		}

		public static void FlushResistanceAndUp (ElectricalOIinheritance Object,  GameObject SourceInstance = null  ){
			if (SourceInstance == null) {
				Logger.Log ("yo do not?");
				if (Object.Data.ResistanceComingFrom.Count > 0) {
					Object.Data.ResistanceComingFrom.Clear ();
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.FlushResistanceAndUp ();
					}
					Object.Data.ResistanceGoingTo.Clear ();
					Object.Data.CurrentGoingTo.Clear ();
					Object.Data.CurrentComingFrom.Clear ();
					Object.Data.SourceVoltages.Clear ();
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
				}

			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.Data.ResistanceComingFrom.ContainsKey (InstanceID) || Object.Data.ResistanceGoingTo.ContainsKey (InstanceID)) {
					Object.Data.ResistanceComingFrom.Remove (InstanceID);
					Object.Data.ResistanceGoingTo.Remove (InstanceID);
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.FlushResistanceAndUp (SourceInstance);
					}
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					Object.Data.SourceVoltages.Remove (InstanceID);
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
				}
			}
		}

		public static void FlushSupplyAndUp (ElectricalOIinheritance Object,GameObject SourceInstance = null ){
			if (SourceInstance == null) {
				if (Object.Data.CurrentComingFrom.Count > 0) {
					Object.Data.CurrentComingFrom.Clear();
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.FlushSupplyAndUp();
					}
					Object.Data.CurrentGoingTo.Clear();
					Object.Data.SourceVoltages.Clear();
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
				}

			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				//Logger.Log(Object.GameObject().name);
				if (Object.Data.CurrentComingFrom.ContainsKey (InstanceID)) {
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.FlushSupplyAndUp (SourceInstance);
					}
				} else if (Object.Data.CurrentGoingTo.ContainsKey (InstanceID)) {
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.FlushSupplyAndUp (SourceInstance);
					}
				}
				Object.Data.CurrentGoingTo.Remove (InstanceID);
				Object.Data.SourceVoltages.Remove (InstanceID);
				ElectricityFunctions.WorkOutActualNumbers (Object);
			}
		}

		public static void RemoveSupply(ElectricalOIinheritance Object,GameObject SourceInstance = null ){		
			if (SourceInstance == null) {
				if (Object.Data.Downstream.Count > 0 || Object.Data.Upstream.Count > 0) {
					Object.Data.Downstream.Clear();
					Object.Data.Upstream.Clear();
					Object.Data.FirstPresent = new int ();
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.RemoveSupply ();
					}
					Object.Data.Upstream.Clear();
					Object.Data.SourceVoltages.Clear();
					Object.Data.ResistanceGoingTo.Clear();
					Object.Data.ResistanceComingFrom.Clear();
					Object.Data.CurrentGoingTo.Clear();
					Object.Data.CurrentComingFrom.Clear();
					Object.Data.SourceVoltages.Clear();
					Object.Data.CurrentInWire = new float ();
					Object.Data.ActualVoltage = new float ();
					Object.Data.EstimatedResistance = new float ();
					Object.Data.UpstreamCount = new int ();
					Object.Data.DownstreamCount = new int ();
					Object.Data.ResistanceToConnectedDevices.Clear();
					Object.connectedDevices.Clear();
				}
			} else {
				int InstanceID = SourceInstance.GetInstanceID ();
				if (Object.Data.Downstream.ContainsKey (InstanceID)) {
					Object.Data.Downstream.Remove (InstanceID);
					if (Object.Data.FirstPresent == InstanceID) {
						Object.Data.FirstPresent = new int ();
					}
					foreach (ElectricalOIinheritance JumpTo in Object.Data.connections) {
						JumpTo.RemoveSupply (SourceInstance);
					}
					if (InstanceID == Object.GameObject ().GetInstanceID ()) {
						CleanConnectedDevicesFromPower (Object);
						Object.Data.ResistanceToConnectedDevices.Clear();
					}
					Object.Data.Upstream.Remove (InstanceID);
					Object.Data.SourceVoltages.Remove (InstanceID); 
					Object.Data.ResistanceGoingTo.Remove (InstanceID);
					Object.Data.ResistanceComingFrom.Remove (InstanceID);
					Object.Data.CurrentGoingTo.Remove (InstanceID);
					Object.Data.CurrentComingFrom.Remove (InstanceID);
					Object.Data.SourceVoltages.Remove (InstanceID);
					ElectricityFunctions.WorkOutActualNumbers(Object);;
					Object.Data.UpstreamCount = new int ();
					Object.Data.DownstreamCount = new int ();
				}
			}
		}
	}
}
