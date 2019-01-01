using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalSynchronisationStorage
{
	public PowerTypeCategory TheCategory;
	public IElectricalNeedUpdate device;
}
public static class ElectricalSynchronisation
{ //What keeps electrical Ticking
	//so this is correlated to what has changed on the network, Needs to be optimised so (when one resistant source changes only that one updates its values currently the entire network updates their values)
	public static bool StructureChange = true; //deals with the connections this will clear them out only
	public static HashSet<IElectricalNeedUpdate> NUStructureChangeReact = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> NUResistanceChange = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> ResistanceChange = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> InitialiseResistanceChange = new HashSet<IElectricalNeedUpdate>();
	public static HashSet<IElectricalNeedUpdate> NUCurrentChange = new HashSet<IElectricalNeedUpdate>();
	private static bool DeadEndSet = false;
	public static DeadEndConnection DeadEnd = new DeadEndConnection(); //so resistance sources coming from itself  like an apc Don't cause loops this is used as coming from and so therefore it is ignored


	public static HashSet<IElectricityIO> DirectionWorkOnNextList = new HashSet<IElectricityIO> ();
	public static HashSet<IElectricityIO> DirectionWorkOnNextListWait = new HashSet<IElectricityIO> ();

	public static HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ResistanceWorkOnNextList = new HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ();
	public static HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ResistanceWorkOnNextListWait = new HashSet<KeyValuePair<IElectricityIO,IElectricityIO>> ();

	public static int currentTick;
	public static float tickRateComplete = 1f; //currently set to update every second
	public static float tickRate;
	private static float tickCount = 0f;

	public static List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>()
	{ //Since you want the batteries to come after the radiation collectors so batteries don't put all there charge out then realise radiation collectors already doing it
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.PowerGenerator,
	};

	public static Dictionary<PowerTypeCategory, HashSet<IElectricalNeedUpdate>> ALiveSupplies = new Dictionary<PowerTypeCategory, HashSet<IElectricalNeedUpdate>>()
	{ //Things that are supplying voltage
	};

	public static HashSet<IElectricalNeedUpdate> PoweredDevices = new HashSet<IElectricalNeedUpdate>(); // things that may need electrical updates to react to voltage changes 
	public static List<ElectricalSynchronisationStorage> ToRemove = new List<ElectricalSynchronisationStorage>();

	public static void AddSupply(IElectricalNeedUpdate Supply, PowerTypeCategory TheCategory)
	{
		if (!(ALiveSupplies.ContainsKey(TheCategory)))
		{
			ALiveSupplies[TheCategory] = new HashSet<IElectricalNeedUpdate>();
		}
		ALiveSupplies[TheCategory].Add(Supply);
	}
	public static void RemoveSupply(IElectricalNeedUpdate Supply, PowerTypeCategory TheCategory)
	{
		ElectricalSynchronisationStorage QuickAdd = new ElectricalSynchronisationStorage();
		QuickAdd.device = Supply;
		QuickAdd.TheCategory = TheCategory;
		ToRemove.Add(QuickAdd);
	}

	public static void DoUpdate()
	{ //The beating heart
		if (!DeadEndSet)
		{
			DeadEnd.Categorytype = PowerTypeCategory.DeadEndConnection; //yeah Class stuff
			DeadEndSet = true;
		}
		if (tickRate == 0)
		{
			tickRate = tickRateComplete / 5;
		}
		tickCount += Time.deltaTime;
		if (tickCount > tickRate)
		{
			tickCount = 0f;
			if (StructureChange && (currentTick == 0))
			{
				StructureChange = false;
				for (int i = 0; i < OrderList.Count; i++)
				{
					if (!(ALiveSupplies.ContainsKey(OrderList[i])))
					{
						ALiveSupplies[OrderList[i]] = new HashSet<IElectricalNeedUpdate>();
					}
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[OrderList[i]])
					{
						TheSupply.PowerUpdateStructureChange();
					}
				}

				foreach (IElectricalNeedUpdate ToWork in PoweredDevices)
				{
					ToWork.PowerUpdateStructureChange();
				}
			}
			else if (currentTick == 1) 
			{ //This will generate directions
				for (int i = 0; i < OrderList.Count; i++)
				{
					if (!(ALiveSupplies.ContainsKey(OrderList[i])))
					{
						ALiveSupplies[OrderList[i]] = new HashSet<IElectricalNeedUpdate>();

					}
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[OrderList[i]])
					{
						if (NUStructureChangeReact.Contains (TheSupply)) {
							TheSupply.PowerUpdateStructureChangeReact();
							NUStructureChangeReact.Remove (TheSupply);
						}
					}
				}
			}
			else if (currentTick == 2)
			{ //Clear  resistance and Calculate the resistance for everything
				foreach (IElectricalNeedUpdate PoweredDevice  in InitialiseResistanceChange) {
					PoweredDevice.InitialPowerUpdateResistance ();
				}
				InitialiseResistanceChange.Clear();
				foreach (IElectricalNeedUpdate PoweredDevice  in ResistanceChange) {
					PoweredDevice.PowerUpdateResistanceChange ();
				}
				ResistanceChange.Clear();
				for (int i = 0; i < OrderList.Count; i++)
				{
					if (!(ALiveSupplies.ContainsKey(OrderList[i])))
					{
						ALiveSupplies[OrderList[i]] = new HashSet<IElectricalNeedUpdate>();
					}
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[OrderList[i]])
					{
						if (NUResistanceChange.Contains (TheSupply) && !(NUStructureChangeReact.Contains (TheSupply))) {
							TheSupply.PowerUpdateResistanceChange();
							NUResistanceChange.Remove (TheSupply);
						}
					}
				}
				CircuitResistanceLoop ();
			}
			else if (currentTick == 3)
			{ // Clear currents and Calculate the currents And voltage

				for (int i = 0; i < OrderList.Count; i++)
				{
					if (!(ALiveSupplies.ContainsKey(OrderList[i])))
					{
						ALiveSupplies[OrderList[i]] = new HashSet<IElectricalNeedUpdate>();

					}
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[OrderList[i]])
					{
						if (NUCurrentChange.Contains (TheSupply) && !(NUStructureChangeReact.Contains (TheSupply)) && !(NUResistanceChange.Contains (TheSupply))) {
							TheSupply.PowerUpdateCurrentChange();
							NUCurrentChange.Remove (TheSupply);
						}
					}
				}
			}
			else if (currentTick == 4)
			{ //Sends updates to things that might need it

				for (int i = 0; i < OrderList.Count; i++)
				{
					if (!(ALiveSupplies.ContainsKey(OrderList[i])))
					{
						ALiveSupplies[OrderList[i]] = new HashSet<IElectricalNeedUpdate>();
					}
					foreach (IElectricalNeedUpdate TheSupply in ALiveSupplies[OrderList[i]])
					{
						TheSupply.PowerNetworkUpdate();
					}
				}
				foreach (IElectricalNeedUpdate ToWork in PoweredDevices)
				{
					ToWork.PowerNetworkUpdate();
				}
			
			}
			if (ToRemove.Count > 0)
			{
				while (ToRemove.Count > 0)
				{
					if (ALiveSupplies.ContainsKey(ToRemove[0].TheCategory))
					{
						if (ALiveSupplies[ToRemove[0].TheCategory].Contains(ToRemove[0].device))
						{
							ALiveSupplies[ToRemove[0].TheCategory].Remove(ToRemove[0].device);
						}
					}
					ToRemove.RemoveAt(0);
				}
			}
			currentTick++;
			if (currentTick > 4)
			{
				currentTick = 0;
			}
		}
	}

//	public static void CircuitSearchLoop(){
//		InputOutputFunctions.DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject(), Thiswire);
//		bool Break = false;
//		List<IElectricityIO> IterateDirectionWorkOnNextList = new List<IElectricityIO> ();
//		while (!Break) {
//			IterateDirectionWorkOnNextList = new List<IElectricityIO> (DirectionWorkOnNextList);
//			DirectionWorkOnNextList.Clear();
//			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
//				IterateDirectionWorkOnNextList [i].DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
//			}
//			if (DirectionWorkOnNextList.Count <= 0) {
//				IterateDirectionWorkOnNextList = new List<IElectricityIO> (DirectionWorkOnNextListWait);
//				DirectionWorkOnNextListWait.Clear();
//				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
//					IterateDirectionWorkOnNextList [i].DirectionOutput (ElectricalSynchronisation.currentTick, Thiswire.GameObject());
//				}
//			}
//			if (DirectionWorkOnNextList.Count <= 0 && DirectionWorkOnNextListWait.Count <= 0) {
//				//Logger.Log ("stop!");
//				Break = true;
//			}
//		}
//	}
		
	public static void CircuitResistanceLoop(){
		bool Break = false;
		//Logger.Log ("CircuitResistanceLoop! ");
		List<KeyValuePair<IElectricityIO,IElectricityIO>> IterateDirectionWorkOnNextList = new List<KeyValuePair<IElectricityIO,IElectricityIO>> ();
		while (!Break) {
			
			IterateDirectionWorkOnNextList = new List<KeyValuePair<IElectricityIO,IElectricityIO>> (ResistanceWorkOnNextList);
			ResistanceWorkOnNextList.Clear();
			//Logger.Log (IterateDirectionWorkOnNextList.Count.ToString () + "IterateDirectionWorkOnNextList.Count");
			for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
				IterateDirectionWorkOnNextList [i].Value.ResistancyOutput (ElectricalSynchronisation.currentTick, IterateDirectionWorkOnNextList [i].Key.GameObject());
			}
			if (ResistanceWorkOnNextList.Count <= 0) {
				IterateDirectionWorkOnNextList = new List<KeyValuePair<IElectricityIO,IElectricityIO>> (ResistanceWorkOnNextListWait);
				ResistanceWorkOnNextListWait.Clear();
				for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++) { 
					IterateDirectionWorkOnNextList [i].Value.ResistancyOutput (ElectricalSynchronisation.currentTick, IterateDirectionWorkOnNextList [i].Key.GameObject());
				}
			}
			if (ResistanceWorkOnNextList.Count <= 0 && ResistanceWorkOnNextListWait.Count <= 0) {
				Break = true;
			}
		}
	}
}