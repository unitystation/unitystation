using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;
using UnityEngine.Profiling;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Profiling;
using System.Linq;

#endif

public class ElectricalSynchronisationStorage
{
	public PowerTypeCategory category;
	public ElectricalNodeControl device;
}

public static class ElectricalSynchronisation
{
	//What keeps electrical Ticking
	//so this is correlated to what has changed on the network, Needs to be optimised so (when one resistant source changes only that one updates its values currently the entire network updates their values)
	public static bool StructureChange = true; //deals with the connections this will clear them out only

	public static HashSet<CableInheritance>
		NUCableStructureChange = new HashSet<CableInheritance>(); //used for tracking cable deconstruction

	public static HashSet<ElectricalNodeControl>
		NUStructureChangeReact =
			new HashSet<ElectricalNodeControl>(); //Used for poking the supplies to make up and down paths all the resistant sources

	public static HashSet<ElectricalNodeControl>
		NUResistanceChange =
			new HashSet<ElectricalNodeControl>(); //Used for all the resistant sources to broadcast there resistance  Used for supplies but could probably be combined with ResistanceChange

	public static HashSet<ElectricalNodeControl> ResistanceChange = new HashSet<ElectricalNodeControl>(); //

	public static HashSet<ElectricalNodeControl>
		InitialiseResistanceChange =
			new HashSet<ElectricalNodeControl>(); //Used for getting stuff to generate constant resistance values not really used properly

	public static HashSet<ElectricalNodeControl> NUCurrentChange = new HashSet<ElectricalNodeControl>(); //
	public static HashSet<CableInheritance> CableUpdates = new HashSet<CableInheritance>();
	public static HashSet<CableInheritance> WorkingCableUpdates = new HashSet<CableInheritance>();
	public static CableInheritance CableToDestroy;
	private static bool Initialise = false;

	public static List<ElectricalOIinheritance> DirectionWorkOnNextList = new List<ElectricalOIinheritance>();
	public static List<ElectricalOIinheritance> DirectionWorkOnNextListWait = new List<ElectricalOIinheritance>();

	public static List<ElectricalOIinheritance> _DirectionWorkOnNextList = new List<ElectricalOIinheritance>();
	public static List<ElectricalOIinheritance> _DirectionWorkOnNextListWait = new List<ElectricalOIinheritance>();
	public static bool UesAlternativeDirectionWorkOnNextList;

	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> ResistanceWorkOnNextList =
		new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();

	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> ResistanceWorkOnNextListWait =
		new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();

	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> _ResistanceWorkOnNextList =
		new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();

	public static List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> _ResistanceWorkOnNextListWait =
		new List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>>();

	public static bool UesAlternativeResistanceWorkOnNextList;


	public static KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> OneJump;
	public static ElectronicSupplyData InputSupplyingUsingData;
	public static ElectronicSupplyData OutputSupplyingUsingData;

	public static int currentTick;
	public static float tickRateComplete = 1f; //currently set to update every second
	public static float tickRate;
	private static float tickCount = 0f;
	private const int Steps = 5;

	public static List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>()
	{
		//Since you want the batteries to come after the radiation collectors so batteries don't put all there charge out then realise radiation collectors already doing it
		PowerTypeCategory.SolarPanel,
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.PowerGenerator,
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.SolarPanelController,
		PowerTypeCategory.DepartmentBattery,
	};

	public static List<PowerTypeCategory> UnconditionalSupplies = new List<PowerTypeCategory>()
	{
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.PowerGenerator,
		PowerTypeCategory.SolarPanel,
	};

	public static HashSet<PowerTypeCategory> ReactiveSuppliesSet = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.SolarPanelController,
	};

	public static HashSet<ElectricalNodeControl> TotalSupplies = new HashSet<ElectricalNodeControl>();

	public static Dictionary<PowerTypeCategory, HashSet<ElectricalNodeControl>> AliveSupplies =
		new Dictionary<PowerTypeCategory, HashSet<ElectricalNodeControl>>()
			{ }; //Things that are supplying voltage

	public static HashSet<ElectricalNodeControl>
		PoweredDevices =
			new HashSet<ElectricalNodeControl>(); // things that may need electrical updates to react to voltage changes

	public static Queue<ElectricalSynchronisationStorage> ToRemove = new Queue<ElectricalSynchronisationStorage>();


#if UNITY_EDITOR
	public const string updateName = nameof(ElectricalSynchronisation) + "." + nameof(DoUpdate);
//	private static ProfilerMarker update = new ProfilerMarker(updateName);

	public static readonly string[] markerNames = new[]
	{
		nameof(IfStructureChange),
		nameof(PowerUpdateStructureChangeReact),
		nameof(PowerUpdateResistanceChange),
		nameof(PowerUpdateCurrentChange),
		nameof(PowerNetworkUpdate),
	}.Select(mn => $"{nameof(ElectricalSynchronisation)}.{mn}").ToArray();

	private static readonly ProfilerMarker[] markers = markerNames.Select(mn => new ProfilerMarker(mn)).ToArray();
#endif

	public static void Reset()
	{
		Initialise = false;
		NUCableStructureChange.Clear();
		NUStructureChangeReact.Clear();
		NUResistanceChange.Clear();
		ResistanceChange.Clear();
		InitialiseResistanceChange.Clear();
		NUCurrentChange.Clear();
		CableUpdates.Clear();
		WorkingCableUpdates.Clear();

		DirectionWorkOnNextList.Clear();
		DirectionWorkOnNextListWait.Clear();
		_DirectionWorkOnNextList.Clear();
		_DirectionWorkOnNextListWait.Clear();

		ResistanceWorkOnNextList.Clear();
		ResistanceWorkOnNextListWait.Clear();
		_ResistanceWorkOnNextList.Clear();
		_ResistanceWorkOnNextListWait.Clear();

		AliveSupplies.Clear();
		TotalSupplies.Clear();
		ToRemove.Clear();
	}

	public static void AddSupply(ElectricalNodeControl Supply, PowerTypeCategory category)
	{
		if (!(AliveSupplies.ContainsKey(category)))
		{
			AliveSupplies[category] = new HashSet<ElectricalNodeControl>();
		}

		AliveSupplies[category].Add(Supply);
		TotalSupplies.Add(Supply);
	}

	public static void RemoveSupply(ElectricalNodeControl Supply, PowerTypeCategory category)
	{
		ElectricalSynchronisationStorage QuickAdd = new ElectricalSynchronisationStorage();
		QuickAdd.device = Supply;
		QuickAdd.category = category;
		ToRemove.Enqueue(QuickAdd);
	}

	public static void DoUpdate()
	{
		//The beating heart
#if UNITY_EDITOR
//		update.Begin();
#endif
		if (!Initialise)
		{
			foreach (var category in OrderList)
			{
				if (!AliveSupplies.ContainsKey(category))
				{
					AliveSupplies[category] = new HashSet<ElectricalNodeControl>();
				}
			}
			
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
#if UNITY_EDITOR
//		update.End();
#endif
	}

	private static void DoTick()
	{
#if UNITY_EDITOR
		using (markers[currentTick].Auto())
#endif
			switch (currentTick)
			{
				case 0:
					IfStructureChange();
					break;
				case 1:
					PowerUpdateStructureChangeReact();
					break;
				case 2:
					PowerUpdateResistanceChange();
					break;
				case 3:
					PowerUpdateCurrentChange();
					break;
				case 4:
					PowerNetworkUpdate();
					break;
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
//		Profiler.BeginSample("IfStructureChange");
		//Logger.Log("PowerUpdateStructureChange", Category.Electrical);
		foreach (CableInheritance cabel in NUCableStructureChange)
		{
			cabel.PowerUpdateStructureChange(); //so Destruction of cables won't trigger the entire thing to refresh saving a bit of performance since they have a bit of code for jumping onto different supplies and  , adding them to NUStructureChangeReact
		}

		NUCableStructureChange.Clear();
		if (!StructureChange)
		{
//			Profiler.EndSample();
			return;
		}

		//Logger.Log("StructureChange bool PowerUpdateStructureChange!",Category.Electrical);
		StructureChange = false;
		foreach (var category in OrderList)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[category])
			{
				TheSupply.PowerUpdateStructureChange();
			}
		}

		foreach (ElectricalNodeControl ToWork in PoweredDevices)
		{
			ToWork.PowerUpdateStructureChange();
		}

//		Profiler.EndSample();
	}

	/// <summary>
	/// This will generate directions
	/// </summary>
	private static void PowerUpdateStructureChangeReact()
	{
//		Profiler.BeginSample("PowerUpdateStructureChangeReact");
		//Logger.Log("PowerUpdateStructureChangeReact", Category.Electrical);
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUStructureChangeReact.Contains(TheSupply))
				{
					//Logger.Log("PowerUpdateStructureChangeReact " + TheSupply, Category.Electrical);
					TheSupply.PowerUpdateStructureChangeReact();
					NUStructureChangeReact.Remove(TheSupply);
				}
			}
		}

//		Profiler.EndSample();
	}

	/// <summary>
	/// Clear  resistance and Calculate the resistance for everything
	/// </summary>
	private static void PowerUpdateResistanceChange()
	{
//		Profiler.BeginSample("PowerUpdateResistanceChange");
		//Logger.Log("PowerUpdateResistanceChange/InitialPowerUpdateResistance", Category.Electrical);
		foreach (ElectricalNodeControl PoweredDevice in InitialiseResistanceChange)
		{
			PoweredDevice.InitialPowerUpdateResistance();
		}

		//Logger.Log("InitialiseResistanceChange");
		InitialiseResistanceChange.Clear();
		foreach (ElectricalNodeControl PoweredDevice in ResistanceChange)
		{
			PoweredDevice.PowerUpdateResistanceChange();
		}

		ResistanceChange.Clear();
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUResistanceChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)))
				{
					TheSupply.PowerUpdateResistanceChange();
					//Logger.Log("PowerUpdateResistanceChange " + TheSupply, Category.Electrical);
					NUResistanceChange.Remove(TheSupply);
				}
			}
		}

		//Logger.Log("CircuitResistanceLoop");
		CircuitResistanceLoop();
//		Profiler.EndSample();
	}

	/// <summary>
	/// Clear currents and Calculate the currents And voltage
	/// </summary>
	private static void PowerUpdateCurrentChange()
	{
//		Profiler.BeginSample("PowerUpdateCurrentChange");
		for (int i = 0; i < UnconditionalSupplies.Count; i++)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUCurrentChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)) &&
				    !(NUResistanceChange.Contains(TheSupply)))
				{
					//Logger.Log("PowerUpdateCurrentChange " + TheSupply, Category.Electrical);
					TheSupply
						.PowerUpdateCurrentChange(); //Does all the updates for the constant sources since they don't have to worry about other supplies being on or off since they just go steaming ahead
					NUCurrentChange.Remove(TheSupply);
				}
			}
		}

		HashSet<ElectricalNodeControl> DoneSupplies = new HashSet<ElectricalNodeControl>();
		ElectricalNodeControl LowestReactive = null;
		int LowestReactiveint = 9999;
		List<ElectricalNodeControl> QToRemove = new List<ElectricalNodeControl>();
		while (NumberOfReactiveSupplies_f() > 0
		) //This is to calculate the lowest number of supplies that are above the reactive supply so therefore the one that needs to be updated first
		{
			foreach (ElectricalNodeControl TheSupply in NUCurrentChange)
			{
				if (!DoneSupplies.Contains(TheSupply))
				{
					if (TotalSupplies.Contains(TheSupply))
					{
						if (ReactiveSuppliesSet.Contains(TheSupply.Node.InData.Categorytype))
						{
							if (NUCurrentChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)) &&
							    !(NUResistanceChange.Contains(TheSupply)))
							{
								if (LowestReactive == null)
								{
									LowestReactive = TheSupply;
									LowestReactiveint = NumberOfReactiveSupplies(TheSupply.Node);
								}
								else if (LowestReactiveint > NumberOfReactiveSupplies(TheSupply.Node))
								{
									LowestReactive = TheSupply;
									LowestReactiveint = NumberOfReactiveSupplies(TheSupply.Node);
								}
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
				//Logger.Log("PowerUpdateCurrentChange " + LowestReactive, Category.Electrical);
				LowestReactive.PowerUpdateCurrentChange();
				NUCurrentChange.Remove(LowestReactive);
				DoneSupplies.Add(LowestReactive);
			}

			LowestReactive = null;
			LowestReactiveint = 9999;
			foreach (ElectricalNodeControl re in QToRemove)
			{
				NUCurrentChange.Remove(re);
			}

			QToRemove = new List<ElectricalNodeControl>();
		}

//		Profiler.EndSample();
	}

	/// <summary>
	/// Sends updates to things that might need it
	/// </summary>
	private static void PowerNetworkUpdate()
	{
//		Profiler.BeginSample("PowerNetworkUpdate");
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[OrderList[i]])
			{
				TheSupply.PowerNetworkUpdate();
			}
		}

		foreach (ElectricalNodeControl ToWork in PoweredDevices)
		{
			ToWork.PowerNetworkUpdate();
		}

		WorkingCableUpdates = new HashSet<CableInheritance>(CableUpdates);
		CableUpdates.Clear();
		foreach (CableInheritance ToWork in WorkingCableUpdates)
		{
			ToWork.PowerNetworkUpdate(); //This is used to update the cables  if they have the current change to detect if there was an overcurrent
		}

		WorkingCableUpdates.Clear();
		if (CableToDestroy != null)
		{
			CableToDestroy.toDestroy();
			CableToDestroy = null;
		}

//		Profiler.EndSample();
	}

	public static int NumberOfReactiveSupplies_f()
	{
		int Counting = 0;
		foreach (ElectricalNodeControl Device in NUCurrentChange)
		{
			if (Device != null)
			{
				if (ReactiveSuppliesSet.Contains(Device.Node.InData.Categorytype))
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
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Device in Devices.Data
			.ResistanceToConnectedDevices)
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
		//Logger.Log("CircuitSearchLoop Enter", Category.Electrical);
		GameObject GameObject = Thiswire.GameObject();

		if (GameObject == null) return;

		InputOutputFunctions.DirectionOutput(GameObject, Thiswire);
		bool Break = true;
		while (Break)
		{
			UesAlternativeDirectionWorkOnNextList = false;
			DOCircuitSearchLoop(GameObject, _DirectionWorkOnNextList, _DirectionWorkOnNextListWait);

			UesAlternativeDirectionWorkOnNextList = true;
			DOCircuitSearchLoop(GameObject, DirectionWorkOnNextList, DirectionWorkOnNextListWait);

			if (DirectionWorkOnNextList.Count <= 0 & DirectionWorkOnNextListWait.Count <= 0 &
			    _DirectionWorkOnNextList.Count <= 0 & _DirectionWorkOnNextListWait.Count <= 0)
			{
				//Logger.Log ("CircuitSearchLoop stop!", Category.Electrical );
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

	public static void DOCircuitSearchLoop(GameObject GameObject,
		List<ElectricalOIinheritance> IterateDirectionWorkOnNextList,
		List<ElectricalOIinheritance> DirectionWorkOnNextListWait)
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

	public static void ResistanceWorkOnNextListADD(
		KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> Thiswire)
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

	public static void ResistanceWorkOnNextListWaitADD(
		KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> Thiswire)
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
		} while (ResistanceWorkOnNextList.Count > 0 | ResistanceWorkOnNextListWait.Count > 0 |
		         _ResistanceWorkOnNextList.Count > 0 | _ResistanceWorkOnNextListWait.Count > 0);
	}

	public static void DOCircuitResistanceLoop(
		List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> IterateDirectionWorkOnNextList,
		List<KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance>> IterateDirectionWorkOnNextListWait)
	{
		foreach (var direction in IterateDirectionWorkOnNextList)
		{
			direction.Value.ResistancyOutput(direction.Key.GameObject());
		}

		IterateDirectionWorkOnNextList.Clear();
		if (ResistanceWorkOnNextList.Count == 0 & _ResistanceWorkOnNextList.Count == 0)
		{
			foreach (KeyValuePair<ElectricalOIinheritance, ElectricalOIinheritance> direction in
				IterateDirectionWorkOnNextListWait)
			{
				direction.Value.ResistancyOutput(direction.Key.GameObject());
			}

			IterateDirectionWorkOnNextListWait.Clear();
		}
	}
}