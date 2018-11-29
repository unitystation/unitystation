using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalSynchronisationStorage {
	public PowerTypeCategory TheCategory; 
	public IElectricalNeedUpdate device;
}
public static class ElectricalSynchronisation {
	public static bool StructureChange = true;
	public static bool StructureChangeReact = false;
	public static bool ResistanceChange = true;
	public static bool CurrentChange = true;
	private static bool DeadEndSet = false;
	public static DeadEndConnection DeadEnd = new DeadEndConnection();

	public static int currentTick;
	public static float tickRateComplete = 1f; //currently set to update every second
	public static float tickRate; 
	private static float tickCount = 0f;

	public static List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>(){
		PowerTypeCategory.RadiationCollector,
		PowerTypeCategory.SMES,
		PowerTypeCategory.DepartmentBattery,
	};

	public static Dictionary<PowerTypeCategory, HashSet<IElectricalNeedUpdate>> ALiveSupplies = new Dictionary<PowerTypeCategory, HashSet<IElectricalNeedUpdate>> (){
	};

	public static HashSet<IElectricalNeedUpdate> PoweredDevices = new HashSet<IElectricalNeedUpdate> ();
	public static List<ElectricalSynchronisationStorage> ToRemove = new List<ElectricalSynchronisationStorage> ();

	public static void AddSupply(IElectricalNeedUpdate Supply,  PowerTypeCategory TheCategory){
		if (!(ALiveSupplies.ContainsKey (TheCategory))) {
			ALiveSupplies [TheCategory] = new HashSet<IElectricalNeedUpdate> ();
		}
		ALiveSupplies [TheCategory].Add (Supply);
	}
	public static void RemoveSupply(IElectricalNeedUpdate Supply,  PowerTypeCategory TheCategory){
		ElectricalSynchronisationStorage QuickAdd = new ElectricalSynchronisationStorage();
		QuickAdd.device = Supply;
		QuickAdd.TheCategory = TheCategory;
		ToRemove.Add (QuickAdd);
	}

	public static void Update() {
		if (!DeadEndSet) {
			DeadEnd.Categorytype = PowerTypeCategory.DeadEndConnection;
			DeadEndSet = true;
		}		if (tickRate == 0) {
			tickRate = tickRateComplete / 5;
		}
		tickCount += Time.deltaTime;
		if (tickCount > tickRate) {
			tickCount = 0f;
			if (StructureChange && (currentTick == 0))  {
				StructureChange = false;
				StructureChangeReact = true;
				foreach (PowerTypeCategory ToWork in OrderList) {
					if (!(ALiveSupplies.ContainsKey (ToWork))) {
						ALiveSupplies [ToWork] = new HashSet<IElectricalNeedUpdate> ();
					} 
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[ToWork]) {
						TheSupply.PowerUpdateStructureChange ();
					} 
				}
				foreach (IElectricalNeedUpdate ToWork in PoweredDevices) {
					ToWork.PowerUpdateStructureChange ();
				}
			}
			else if (StructureChangeReact && (currentTick == 1) && (!StructureChange)) {
				StructureChangeReact = false;
				foreach (PowerTypeCategory ToWork in OrderList) {
					if (!(ALiveSupplies.ContainsKey (ToWork))) {
						ALiveSupplies [ToWork] = new HashSet<IElectricalNeedUpdate> ();

					} 
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[ToWork]) {
						TheSupply.PowerUpdateStructureChangeReact ();
					} 
				}
			}
			else if (ResistanceChange && (currentTick == 2) && (!(StructureChange || StructureChangeReact))) {
				ResistanceChange = false;
				foreach (PowerTypeCategory ToWork in OrderList) {
					if (!(ALiveSupplies.ContainsKey (ToWork))) {
						ALiveSupplies [ToWork] = new HashSet<IElectricalNeedUpdate> ();
					} 
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[ToWork]) {
						TheSupply.PowerUpdateResistanceChange ();
					} 
				}
			}
			else if  (CurrentChange && (currentTick == 3) && (!(StructureChange || StructureChangeReact  || ResistanceChange))) {
				CurrentChange = false;
				foreach (PowerTypeCategory ToWork in OrderList) {
					if (!(ALiveSupplies.ContainsKey (ToWork))) {
						ALiveSupplies [ToWork] = new HashSet<IElectricalNeedUpdate> ();

					} 
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[ToWork]) {
						TheSupply.PowerUpdateCurrentChange ();
					}
				}
			}
			else if (currentTick == 4) {
				foreach (PowerTypeCategory ToWork in OrderList) {
					if (!(ALiveSupplies.ContainsKey (ToWork))) {
						ALiveSupplies [ToWork] = new HashSet<IElectricalNeedUpdate> ();
					} 
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[ToWork]) {
						TheSupply.PowerNetworkUpdate ();
					} 
				}
				foreach (IElectricalNeedUpdate ToWork in PoweredDevices) {
					ToWork.PowerNetworkUpdate ();
				}
				if (ToRemove.Count > 0) {
					while (ToRemove.Count > 0) {
						if (ALiveSupplies.ContainsKey (ToRemove[0].TheCategory)) {
							if (ALiveSupplies [ToRemove[0].TheCategory].Contains (ToRemove[0].device)) {
								ALiveSupplies [ToRemove[0].TheCategory].Remove(ToRemove[0].device);
							}
						}
						ToRemove.RemoveAt(0);
					}
				}
			}
			currentTick++;
			if (currentTick > 4) {
				currentTick = 0;
			}
		}
	}
}
