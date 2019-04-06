using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalSynchronisationStorage
{
	public PowerTypeCategory category;
	public PowerSupplyControlInheritance device;
}
public static class ElectricalSynchronisation
{
	//What keeps electrical Ticking
	//so this is correlated to what has changed on the network, Needs to be optimised so (when one resistant source changes only that one updates its values currently the entire network updates their values)
	public static bool StructureChange = true; //deals with the connections this will clear them out only
	public static HashSet<PowerSupplyControlInheritance> NUStructureChangeReact = new HashSet<PowerSupplyControlInheritance>();
	public static HashSet<PowerSupplyControlInheritance> NUResistanceChange = new HashSet<PowerSupplyControlInheritance>();
	public static HashSet<PowerSupplyControlInheritance> ResistanceChange = new HashSet<PowerSupplyControlInheritance>();
	public static HashSet<PowerSupplyControlInheritance> InitialiseResistanceChange = new HashSet<PowerSupplyControlInheritance>();
	public static HashSet<PowerSupplyControlInheritance> NUCurrentChange = new HashSet<PowerSupplyControlInheritance>();
	private static bool Initialise = false;
	public static DeadEndConnection DeadEnd = new DeadEndConnection(); //so resistance sources coming from itself  like an apc Don't cause loops this is used as coming from and so therefore it is ignored


	public static List<ElectricalOIinheritance> DirectionWorkOnNextList = new List<ElectricalOIinheritance>();
	public static List<ElectricalOIinheritance> DirectionWorkOnNextListWait = new List<ElectricalOIinheritance>();

	public static List<ElectricalOIinheritance> _DirectionWorkOnNextList = new List<ElectricalOIinheritance>();
	public static List<ElectricalOIinheritance> _DirectionWorkOnNextListWait = new List<ElectricalOIinheritance>();

	public static bool UesAlternativeDirectionWorkOnNextList;

	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> ResistanceWorkOnNextList = new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();
	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> ResistanceWorkOnNextListWait = new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();

	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> _ResistanceWorkOnNextList = new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();
	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> _ResistanceWorkOnNextListWait = new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();

	public static bool UesAlternativeResistanceWorkOnNextList;

	public static int currentTick;
	public static float tickRateComplete = 1f; //currently set to update every second
	public static float tickRate;
	private static float tickCount = 0f;
	private const int Steps = 5;

	public static List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>()
	{ //Since you want the batteries to come after the radiation collectors so batteries don't put all there charge out then realise radiation collectors already doing it
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.PowerGenerator,
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.DepartmentBattery,

	};
	public static List<PowerTypeCategory> WireConnectRelated = new List<PowerTypeCategory>()
	{
		PowerTypeCategory.LowVoltageCable,
		PowerTypeCategory.LowMachineConnector,
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.MediumMachineConnector,
		PowerTypeCategory.HighVoltageCable,
		PowerTypeCategory.HighVoltageCable,

	};
	public static List<PowerTypeCategory> UnconditionalSupplies = new List<PowerTypeCategory>()
	{
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.PowerGenerator,

	};
	public static List<PowerTypeCategory> ReactiveSupplies = new List<PowerTypeCategory>()
	{
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.DepartmentBattery,

	};
	public static HashSet<PowerTypeCategory> ReactiveSuppliesSet = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.DepartmentBattery,

	};
	public static HashSet<PowerSupplyControlInheritance> TotalSupplies = new HashSet<PowerSupplyControlInheritance>();
	public static Dictionary<PowerTypeCategory, HashSet<PowerSupplyControlInheritance>> AliveSupplies = new Dictionary<PowerTypeCategory, HashSet<PowerSupplyControlInheritance>>()
	{ //Things that are supplying voltage
	};

	public static HashSet<PowerSupplyControlInheritance> PoweredDevices = new HashSet<PowerSupplyControlInheritance>(); // things that may need electrical updates to react to voltage changes 
	public static Queue<ElectricalSynchronisationStorage> ToRemove = new Queue<ElectricalSynchronisationStorage>();

	public static void AddSupply(PowerSupplyControlInheritance Supply, PowerTypeCategory category)
	{
		if (!(AliveSupplies.ContainsKey(category)))
		{
			AliveSupplies[category] = new HashSet<PowerSupplyControlInheritance>();
		}
		AliveSupplies[category].Add(Supply);
		TotalSupplies.Add(Supply);
	}
	public static void RemoveSupply(PowerSupplyControlInheritance Supply, PowerTypeCategory category)
	{
		ElectricalSynchronisationStorage QuickAdd = new ElectricalSynchronisationStorage();
		QuickAdd.device = Supply;
		QuickAdd.category = category;
		ToRemove.Enqueue(QuickAdd);
	}

	public static void DoUpdate()
	{ //The beating heart
		if (!Initialise)
		{
			foreach (var category in OrderList)
			{
				if (!AliveSupplies.ContainsKey(category))
				{
					AliveSupplies[category] = new HashSet<PowerSupplyControlInheritance>();
				}
			}
			DeadEnd.InData.Categorytype = PowerTypeCategory.DeadEndConnection; //yeah Class stuff
			Initialise = true;
		}

		if (tickRate == 0)
		{
			tickRate = tickRateComplete / Steps;
		}

		tickCount += Time.deltaTime;

		if (tickCount > tickRate)
		{
			DoTick();
			tickCount = 0f;
			currentTick = ++currentTick % Steps;
		}
	}

	private static void DoTick()
	{
		switch (currentTick)
		{
			case 0: IfStructureChange(); break;
			case 1: PowerUpdateStructureChangeReact(); break;
			case 2: PowerUpdateResistanceChange(); break;
			case 3: PowerUpdateCurrentChange(); break;
			case 4: PowerNetworkUpdate(); break;
		}

		RemoveSupplies();
	}

	/// <summary>
	/// Remove all devices from <see cref="AliveSupplies"/> that were enqueued in <see cref="ToRemove"/>
	/// </summary>
	private static void RemoveSupplies()
	{
		while (ToRemove.Count > 0)
		{
			var toRemove = ToRemove.Dequeue();
			if (AliveSupplies.ContainsKey(toRemove.category) &&
				AliveSupplies[toRemove.category].Contains(toRemove.device))
			{
				AliveSupplies[toRemove.category].Remove(toRemove.device);
				TotalSupplies.Remove(toRemove.device);
			}
		}
	}

	private static void IfStructureChange()
	{
		if (!StructureChange) return;
		//Logger.Log("PowerUpdateStructureChange");
		StructureChange = false;
		foreach (var category in OrderList)
		{
			foreach (PowerSupplyControlInheritance TheSupply in AliveSupplies[category])
			{
				TheSupply.PowerUpdateStructureChange();
			}
		}

		foreach (PowerSupplyControlInheritance ToWork in PoweredDevices)
		{
			ToWork.PowerUpdateStructureChange();
		}
	}

	/// <summary>
	/// This will generate directions
	/// </summary>
	private static void PowerUpdateStructureChangeReact()
	{
		//Logger.Log("PowerUpdateStructureChangeReact");
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (PowerSupplyControlInheritance TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUStructureChangeReact.Contains(TheSupply))
				{
					TheSupply.PowerUpdateStructureChangeReact();
					NUStructureChangeReact.Remove(TheSupply);
				}
			}
		}
	}

	/// <summary>
	/// Clear  resistance and Calculate the resistance for everything
	/// </summary>
	private static void PowerUpdateResistanceChange()
	{
		//Logger.Log("PowerUpdateResistanceChange/InitialPowerUpdateResistance");
		foreach (PowerSupplyControlInheritance PoweredDevice in InitialiseResistanceChange)
		{
			PoweredDevice.InitialPowerUpdateResistance();
		}
		//Logger.Log("InitialiseResistanceChange");
		InitialiseResistanceChange.Clear();
		foreach (PowerSupplyControlInheritance PoweredDevice in ResistanceChange)
		{
			PoweredDevice.PowerUpdateResistanceChange();
		}
		ResistanceChange.Clear();
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (PowerSupplyControlInheritance TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUResistanceChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)))
				{
					TheSupply.PowerUpdateResistanceChange();
					NUResistanceChange.Remove(TheSupply);
				}
			}
		}
		//Logger.Log("CircuitResistanceLoop");
		CircuitResistanceLoop();
	}

	/// <summary>
	/// Clear currents and Calculate the currents And voltage
	/// </summary>
	private static void PowerUpdateCurrentChange()
	{
		for (int i = 0; i < UnconditionalSupplies.Count; i++)
		{
			foreach (PowerSupplyControlInheritance TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUCurrentChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)) && !(NUResistanceChange.Contains(TheSupply)))
				{
					TheSupply.PowerUpdateCurrentChange();
					NUCurrentChange.Remove(TheSupply);
				}
			}
		}
		HashSet<PowerSupplyControlInheritance> DoneSupplies = new HashSet<PowerSupplyControlInheritance>();
		PowerSupplyControlInheritance LowestReactive = null;
		int LowestReactiveint = 9999;
		List<PowerSupplyControlInheritance> QToRemove = new List<PowerSupplyControlInheritance>();
		while (NumberOfReactiveSupplies_f() > 0)
		{
			//Logger.Log("NUCurrentChange.Count > 0");
			foreach (PowerSupplyControlInheritance TheSupply in NUCurrentChange)
			{
				if (!DoneSupplies.Contains(TheSupply))
				{
					if (TotalSupplies.Contains(TheSupply))
					{
						if (ReactiveSuppliesSet.Contains(TheSupply._IElectricityIO.InData.Categorytype))
						{
							if (NUCurrentChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)) && !(NUResistanceChange.Contains(TheSupply)))
							{

								if (LowestReactive == null)
								{
									LowestReactive = TheSupply;
									LowestReactiveint = NumberOfReactiveSupplies(TheSupply._IElectricityIO);
								}
								else if (LowestReactiveint > NumberOfReactiveSupplies(TheSupply._IElectricityIO))
								{
									LowestReactive = TheSupply;
									LowestReactiveint = NumberOfReactiveSupplies(TheSupply._IElectricityIO);
								}
								//TheSupply.GameObject().GetComponent<ElectricalOIinheritance>().Data.ResistanceToConnectedDevices
							}
							else
							{
								QToRemove.Add(TheSupply);
							}
						}
					}
					else
					{
						QToRemove.Add(TheSupply);
					}
				}
				else
				{
					QToRemove.Add(TheSupply);
				}

			}
			if (LowestReactive != null)
			{
				LowestReactive.PowerUpdateCurrentChange();
				NUCurrentChange.Remove(LowestReactive);
				DoneSupplies.Add(LowestReactive);
			}

			LowestReactive = null;
			LowestReactiveint = 9999;
			foreach (PowerSupplyControlInheritance re in QToRemove)
			{
				NUCurrentChange.Remove(re);
			}
			QToRemove = new List<PowerSupplyControlInheritance>();
		}
	}

	/// <summary>
	/// Sends updates to things that might need it
	/// </summary>
	private static void PowerNetworkUpdate()
	{
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (PowerSupplyControlInheritance TheSupply in AliveSupplies[OrderList[i]])
			{
				TheSupply.PowerNetworkUpdate();
			}
		}
		foreach (PowerSupplyControlInheritance ToWork in PoweredDevices)
		{
			ToWork.PowerNetworkUpdate();
		}
	}

	public static int NumberOfReactiveSupplies_f()
	{
		int Counting = 0;
		foreach (PowerSupplyControlInheritance Device in NUCurrentChange)
		{
			if (Device != null)
			{
				if (ReactiveSuppliesSet.Contains(Device._IElectricityIO.InData.Categorytype))
				{
					Counting++;
				}
			}
		}
		return (Counting);
	}

	public static int NumberOfReactiveSupplies(ElectricalOIinheritance Devices)
	{
		int Counting = 0;
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Device in Devices.Data.ResistanceToConnectedDevices)
		{
			if (ReactiveSuppliesSet.Contains(Device.Key.InData.Categorytype))
			{
				Counting++;
			}
		}
		return (Counting);
	}




	public static void CircuitSearchLoop(ElectricalOIinheritance Thiswire)
	{
		GameObject GameObject = Thiswire.GameObject();
		InputOutputFunctions.DirectionOutput(GameObject, Thiswire);
		bool Break = true;
		while (Break)
		{
			UesAlternativeDirectionWorkOnNextList = false;
			DOCircuitSearchLoop(GameObject, _DirectionWorkOnNextList, _DirectionWorkOnNextListWait);

			UesAlternativeDirectionWorkOnNextList = true;
			DOCircuitSearchLoop(GameObject, DirectionWorkOnNextList, DirectionWorkOnNextListWait);

			if (DirectionWorkOnNextList.Count <= 0 & DirectionWorkOnNextListWait.Count <= 0 & _DirectionWorkOnNextList.Count <= 0 & _DirectionWorkOnNextListWait.Count <= 0)
			{
				//Logger.Log ("stop!");
				Break = false;
			}
		}
	}

	public static void DirectionWorkOnNextListADD(ElectricalOIinheritance Thiswire)
	{
		if (UesAlternativeDirectionWorkOnNextList)
		{
			_DirectionWorkOnNextList.Add(Thiswire);
		}
		else
		{
			DirectionWorkOnNextList.Add(Thiswire);
		}
	}
	public static void DirectionWorkOnNextListWaitADD(ElectricalOIinheritance Thiswire)
	{
		if (UesAlternativeDirectionWorkOnNextList)
		{
			_DirectionWorkOnNextListWait.Add(Thiswire);
		}
		else
		{
			DirectionWorkOnNextListWait.Add(Thiswire);
		}
	}

	public static void DOCircuitSearchLoop(GameObject GameObject, List<ElectricalOIinheritance> IterateDirectionWorkOnNextList, List<ElectricalOIinheritance> DirectionWorkOnNextListWait)
	{
		for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++)
		{
			IterateDirectionWorkOnNextList[i].DirectionOutput(GameObject);
		}
		IterateDirectionWorkOnNextList.Clear();
		if (DirectionWorkOnNextList.Count <= 0 & _DirectionWorkOnNextList.Count <= 0)
		{
			for (int i = 0; i < DirectionWorkOnNextListWait.Count; i++)
			{
				DirectionWorkOnNextListWait[i].DirectionOutput(GameObject);
			}
			DirectionWorkOnNextListWait.Clear();
		}
	}

	public static void ResistanceWorkOnNextListADD(KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> Thiswire)
	{
		if (UesAlternativeResistanceWorkOnNextList)
		{
			_ResistanceWorkOnNextList.Add(Thiswire);
		}
		else
		{
			ResistanceWorkOnNextList.Add(Thiswire);
		}
	}
	public static void ResistanceWorkOnNextListWaitADD(KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> Thiswire)
	{
		if (UesAlternativeResistanceWorkOnNextList)
		{
			_ResistanceWorkOnNextListWait.Add(Thiswire);
		}
		else
		{
			ResistanceWorkOnNextListWait.Add(Thiswire);
		}
	}

	public static void CircuitResistanceLoop()
	{
		do
		{
			UesAlternativeResistanceWorkOnNextList = false;
			DOCircuitResistanceLoop(_ResistanceWorkOnNextList, _ResistanceWorkOnNextListWait);

			UesAlternativeResistanceWorkOnNextList = true;
			DOCircuitResistanceLoop(ResistanceWorkOnNextList, ResistanceWorkOnNextListWait);
			 
		} while (ResistanceWorkOnNextList.Count > 0 | ResistanceWorkOnNextListWait.Count > 0 | _ResistanceWorkOnNextList.Count > 0 | _ResistanceWorkOnNextListWait.Count > 0);
	}

	public static void DOCircuitResistanceLoop(List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> IterateDirectionWorkOnNextList,
											   List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> IterateDirectionWorkOnNextListWait)
	{
		foreach (var direction in IterateDirectionWorkOnNextList)
		{
			direction.Value.ResistancyOutput(direction.Key.GameObject());
		}
		IterateDirectionWorkOnNextList.Clear();
		if (ResistanceWorkOnNextList.Count <= 0 & _ResistanceWorkOnNextList.Count <= 0)
		{
			foreach (var direction in IterateDirectionWorkOnNextListWait)
			{
				direction.Value.ResistancyOutput(direction.Key.GameObject());
			}
			IterateDirectionWorkOnNextListWait.Clear();
		}

	}

}