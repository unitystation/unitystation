using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Threading;
using System.Diagnostics;
using System.Linq;
#if UNITY_EDITOR
using Unity.Profiling;
#endif

public class ElectricalSynchronisationStorage
{
	public PowerTypeCategory category;
	public ElectricalNodeControl device;
}

public static class ElectricalSynchronisation
{
	private static bool running;

	public static bool MainThreadProcess = false;

	private static Stopwatch StopWatch = new Stopwatch();

	private static int MillieSecondDelay;

	public static UnityEngine.Object Electriclock = new UnityEngine.Object();

	private static CustomSampler sampler;

	static ElectricalSynchronisation()
	{
		sampler = CustomSampler.Create("ElectricalStep");
	}


	public static void Start()
	{
		if (!running)
		{
			new Thread(Run).Start();

			running = true;
		}
	}

	public static void Stop()
	{
		running = false;
	}

	public static void SetSpeed(int inMillieSecondDelay)
	{
		MillieSecondDelay = inMillieSecondDelay;
	}

	public static void RunStep(bool Thread = true)
	{
		DoUpdate(Thread);
	}

	private static void Run()
	{
		//Profiler.BeginThreadProfiling("Unitystation", "Electronics");
		while (running)
		{
		//	sampler.Begin();
			StopWatch.Restart();
			RunStep();
			StopWatch.Stop();
		//	sampler.End();
			if (StopWatch.ElapsedMilliseconds < MillieSecondDelay)
			{
				Thread.Sleep(MillieSecondDelay - (int)StopWatch.ElapsedMilliseconds);
			}
		}
		//Profiler.EndThreadProfiling();
	}

	//What keeps electrical Ticking
	//so this is correlated to what has changed on the network, Needs to be optimised so (when one resistant source changes only that one updates its values currently the entire network updates their values)
	public static bool StructureChange = true; //deals with the connections this will clear them out only

	public static HashSet<IntrinsicElectronicData>
		NUElectricalObjectsToDestroy = new HashSet<IntrinsicElectronicData>(); //used for tracking deconstruction //#

	public static HashSet<ElectricalNodeControl>
		NUStructureChangeReact =
			new HashSet<ElectricalNodeControl>(); //Used for poking the supplies to make up and down paths all the resistant sources

	public static HashSet<ElectricalNodeControl>
		NUResistanceChange =
			new HashSet<ElectricalNodeControl>(); //Used for all the resistant sources to broadcast there resistance  Used for supplies but could probably be combined with ResistanceChange

	public static HashSet<ElectricalNodeControl> ResistanceChange = new HashSet<ElectricalNodeControl>();

	public static HashSet<ElectricalNodeControl>
		InitialiseResistanceChange =
			new HashSet<ElectricalNodeControl>(); //Used for getting stuff to generate constant resistance values not really used properly

	public static HashSet<ElectricalNodeControl> NUCurrentChange = new HashSet<ElectricalNodeControl>();
	public static HashSet<CableInheritance> CableUpdates = new HashSet<CableInheritance>();//#
	public static CableInheritance CableToDestroy; //#

	private static bool Initialise = false;

	public static List<IntrinsicElectronicData> DirectionWorkOnNextList = new List<IntrinsicElectronicData>(); //#
	public static List<IntrinsicElectronicData> DirectionWorkOnNextListWait = new List<IntrinsicElectronicData>(); //#

	public static List<IntrinsicElectronicData> _DirectionWorkOnNextList = new List<IntrinsicElectronicData>(); //#
	public static List<IntrinsicElectronicData> _DirectionWorkOnNextListWait = new List<IntrinsicElectronicData>(); //#
	public static bool UesAlternativeDirectionWorkOnNextList;

	public static List<QEntry> ResistanceWorkOnNextList =
		new List<QEntry>(); //#

	public static List<QEntry> ResistanceWorkOnNextListWait =
		new List<QEntry>(); //#

	public static List<QEntry> _ResistanceWorkOnNextList =
		new List<QEntry>(); //#

	public static List<QEntry> _ResistanceWorkOnNextListWait =
		new List<QEntry>(); //#

	public static bool UesAlternativeResistanceWorkOnNextList;

	public static int currentTick;
	public static float tickRateComplete = 1f; //currently set to update every second
	public static float tickRate;
	private static float tickCount = 0f;
	private const int Steps = 5;

	public static List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>()
	{
		//Since you want the batteries to come after the radiation collectors so batteries don't put all there charge out then realise radiation collectors already doing it
		PowerTypeCategory.SolarPanel,
		PowerTypeCategory.RadiationCollector,
		PowerTypeCategory.PowerGenerator,//make sure unconditional supplies come first

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

	public static List<QueueAddSupply> SupplyToadd = new List<QueueAddSupply>();

	public static HashSet<ElectricalNodeControl>
		PoweredDevices =
			new HashSet<ElectricalNodeControl>(); // things that may need electrical updates to react to voltage changes

	public static Queue<ElectricalSynchronisationStorage> ToRemove = new Queue<ElectricalSynchronisationStorage>();

	public struct QueueAddSupply
	{
		public ElectricalNodeControl supply;
		public PowerTypeCategory category;
	};

#if UNITY_EDITOR
	public const string updateName = nameof(ElectricalSynchronisation) + "." + nameof(DoUpdate);

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
		NUElectricalObjectsToDestroy.Clear();
		NUStructureChangeReact.Clear();
		NUResistanceChange.Clear();
		ResistanceChange.Clear();
		InitialiseResistanceChange.Clear();
		NUCurrentChange.Clear();
		CableUpdates.Clear();

		DirectionWorkOnNextList.Clear();
		DirectionWorkOnNextListWait.Clear();
		_DirectionWorkOnNextList.Clear();
		_DirectionWorkOnNextListWait.Clear();

		ResistanceWorkOnNextList.Clear();
		ResistanceWorkOnNextListWait.Clear();
		_ResistanceWorkOnNextList.Clear();
		_ResistanceWorkOnNextListWait.Clear();

		SupplyToadd.Clear();
		AliveSupplies.Clear();
		TotalSupplies.Clear();
		ToRemove.Clear();
	}

	public static void AddSupply(ElectricalNodeControl Supply, PowerTypeCategory category)
	{
		//SupplyToadd
		var Adding = new QueueAddSupply()
		{
			category = category,
			supply = Supply
		};
		SupplyToadd.Add(Adding);
	}

	private static void InternalAddSupply(QueueAddSupply Adding)
	{
		if (!(AliveSupplies.ContainsKey(Adding.category)))
		{
			AliveSupplies[Adding.category] = new HashSet<ElectricalNodeControl>();
		}
		AliveSupplies[Adding.category].Add(Adding.supply);
		TotalSupplies.Add(Adding.supply);
	}

	public static void RemoveSupply(ElectricalNodeControl Supply, PowerTypeCategory category)
	{
		ElectricalSynchronisationStorage QuickAdd = new ElectricalSynchronisationStorage();
		QuickAdd.device = Supply;
		QuickAdd.category = category;
		ToRemove.Enqueue(QuickAdd);
	}

	public static void DoUpdate(bool Thread = true)
	{
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
		currentTick = ++currentTick % Steps;
		DoTick(Thread);
	}

	private static void DoTick(bool Thread = true)
	{
#if UNITY_EDITOR
	//	using (markers[currentTick].Auto())
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
					if (Thread)
					{
						ThreadedPowerNetworkUpdate();
					}
					else {
						PowerNetworkUpdate();
					}
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
		ElectricalPool.PoolsStatuses();
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
		//Logger.Log("PowerUpdateCurrentChange ", Category.Electrical);
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
									LowestReactiveint = NumberOfReactiveSupplies(TheSupply.Node.InData);
								}
								else if (LowestReactiveint > NumberOfReactiveSupplies(TheSupply.Node.InData))
								{
									LowestReactive = TheSupply;
									LowestReactiveint = NumberOfReactiveSupplies(TheSupply.Node.InData);
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

	private static void ThreadedPowerNetworkUpdate()
	{
		lock (Electriclock)
		{
			MainThreadProcess = true;
			Monitor.Wait(Electriclock);
		}
	}


	/// <summary>
	/// Sends updates to things that might need it
	/// </summary>
	public static void PowerNetworkUpdate()
	{
		//Logger.Log("PowerNetworkUpdate", Category.Electrical);
		//		Profiler.BeginSample("PowerNetworkUpdate");
		for (int i = 0; i < SupplyToadd.Count; i++)
		{
			InternalAddSupply(SupplyToadd[i]);
		}
		SupplyToadd.Clear();

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

		foreach (CableInheritance ToWork in CableUpdates)
		{
			ToWork.PowerNetworkUpdate();
		}
		CableUpdates.Clear();


		if (CableToDestroy != null)
		{
			CableToDestroy.wireConnect.DestroyThisPlease();
			CableToDestroy = null;
		}

		//Structure change and stuff
		foreach (var Thing in NUElectricalObjectsToDestroy)
		{
			Thing.DestroyingThisNow();
		}
		NUElectricalObjectsToDestroy.Clear();
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

	public static int NumberOfReactiveSupplies(IntrinsicElectronicData Devices)
	{
		int Counting = 0;
		foreach (var Device in Devices.Data
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

		InputOutputFunctions.DirectionOutput(Thiswire, Thiswire.InData);
		bool Break = true;
		while (Break)
		{
			UesAlternativeDirectionWorkOnNextList = false;
			DOCircuitSearchLoop(Thiswire, _DirectionWorkOnNextList, _DirectionWorkOnNextListWait);

			UesAlternativeDirectionWorkOnNextList = true;
			DOCircuitSearchLoop(Thiswire, DirectionWorkOnNextList, DirectionWorkOnNextListWait);

			if (DirectionWorkOnNextList.Count <= 0 & DirectionWorkOnNextListWait.Count <= 0 &
				_DirectionWorkOnNextList.Count <= 0 & _DirectionWorkOnNextListWait.Count <= 0)
			{
				//Logger.Log ("CircuitSearchLoop stop!", Category.Electrical );
				Break = false;
			}
		}
	}

	public static void DirectionWorkOnNextListADD(IntrinsicElectronicData Thiswire)
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

	public static void DirectionWorkOnNextListWaitADD(IntrinsicElectronicData Thiswire)
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

	public static void DOCircuitSearchLoop(ElectricalOIinheritance GameObject,
		List<IntrinsicElectronicData> IterateDirectionWorkOnNextList,
		List<IntrinsicElectronicData> DirectionWorkOnNextListWait)
	{
		for (int i = 0; i < IterateDirectionWorkOnNextList.Count; i++)
		{
			IterateDirectionWorkOnNextList[i].DirectionOutput(GameObject);
		}

		IterateDirectionWorkOnNextList.Clear();
		if (DirectionWorkOnNextList.Count <= 0 && _DirectionWorkOnNextList.Count <= 0)
		{
			for (int i = 0; i < DirectionWorkOnNextListWait.Count; i++)
			{
				DirectionWorkOnNextListWait[i].DirectionOutput(GameObject);
			}

			DirectionWorkOnNextListWait.Clear();
		}
	}

	public static void ResistanceWorkOnNextListADD(
		QEntry Thiswire)
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
		QEntry Thiswire)
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
		List<QEntry> IterateDirectionWorkOnNextList,
		List<QEntry> IterateDirectionWorkOnNextListWait)
	{
		/*foreach (var direction in IterateDirectionWorkOnNextList)
		{
			direction.InData.ResistancyOutput(direction.OIinheritance);
		}

		ElectricalPool.PooledQEntry.AddRange(IterateDirectionWorkOnNextList);
		IterateDirectionWorkOnNextList.Clear();

		if (ResistanceWorkOnNextList.Count == 0 & _ResistanceWorkOnNextList.Count == 0)
		{Get
			foreach (QEntry direction in IterateDirectionWorkOnNextListWait)
			{
				direction.InData.ResistancyOutput(direction.OIinheritance);
			}
			ElectricalPool.PooledQEntry.AddRange(IterateDirectionWorkOnNextListWait);
			IterateDirectionWorkOnNextListWait.Clear();
		}*/
	}




	//public static ElectricalDirectionStep GetStep()
	//{
	//	if (PooledSteps.Count > 0)
	//	{
	//		var electricalDirectionStep = PooledSteps[0];
	//		PooledSteps.RemoveAt(0);
	//		electricalDirectionStep.Clean();
	//		return (electricalDirectionStep);
	//	}
	//	else {
	//		return (new ElectricalDirectionStep());
	//	}

	//}intrinsic


	public class QEntry{
		public ElectricalOIinheritance OIinheritance;
		public IntrinsicElectronicData InData;
	}
}