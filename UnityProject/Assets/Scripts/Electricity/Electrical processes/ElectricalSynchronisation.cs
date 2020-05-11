using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using System;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using Unity.Profiling;

#endif

public class ElectricalSynchronisationStorage
{
	public PowerTypeCategory category;
	public ElectricalNodeControl device;
}

public class ElectricalSynchronisation : MonoBehaviour
{
	private bool running;

	public bool MainThreadProcess = false;

	private Stopwatch StopWatch = new Stopwatch();

	private int MillieSecondDelay;

	private CustomSampler sampler;

	public Thread thread;

	ElectricalSynchronisation()
	{
		sampler = CustomSampler.Create("ElectricalStep");
	}

	public void StartSim()
	{
		if (!running)
		{
			running = true;
			thread = new Thread(Run);
			thread.Start();
		}
	}

	public void StopSim()
	{
		running = false;
	}

	public void SetSpeed(int inMillieSecondDelay)
	{
		MillieSecondDelay = inMillieSecondDelay;
	}

	public void RunStep(bool Thread = true)
	{
		DoUpdate(Thread);
	}

	private void Run()
	{
		Profiler.BeginThreadProfiling("Unitystation", "Electronics");
		while (running)
		{
			sampler.Begin();
			StopWatch.Restart();
			RunStep();
			StopWatch.Stop();
			sampler.End();
			if (StopWatch.ElapsedMilliseconds < MillieSecondDelay)
			{
				Thread.Sleep(MillieSecondDelay - (int) StopWatch.ElapsedMilliseconds);
			}
		}
		Profiler.EndThreadProfiling();
		thread.Abort();
	}

	//What keeps electrical Ticking
	//so this is correlated to what has changed on the network, Needs to be optimised so (when one resistant source changes only that one updates its values currently the entire network updates their values)
	public bool StructureChange = true; //deals with the connections this will clear them out only

	public HashSet<IntrinsicElectronicData>
		NUElectricalObjectsToDestroy = new HashSet<IntrinsicElectronicData>(); //used for tracking deconstruction //#

	public HashSet<ElectricalNodeControl>
		NUStructureChangeReact =
			new HashSet<ElectricalNodeControl>(); //Used for poking the supplies to make up and down paths all the resistant sources

	public HashSet<ElectricalNodeControl>
		NUResistanceChange =
			new HashSet<ElectricalNodeControl>(); //Used for all the resistant sources to broadcast there resistance  Used for supplies but could probably be combined with ResistanceChange

	public HashSet<ElectricalNodeControl> ResistanceChange = new HashSet<ElectricalNodeControl>();

	public HashSet<ElectricalNodeControl>
		InitialiseResistanceChange =
			new HashSet<ElectricalNodeControl>(); //Used for getting stuff to generate constant resistance values not really used properly

	public HashSet<ElectricalNodeControl> NUCurrentChange = new HashSet<ElectricalNodeControl>();
	public HashSet<CableInheritance> CableUpdates = new HashSet<CableInheritance>(); //#
	public CableInheritance CableToDestroy; //#

	private bool Initialise = false;

	public List<IntrinsicElectronicData> DirectionWorkOnNextList = new List<IntrinsicElectronicData>(); //#
	public List<IntrinsicElectronicData> DirectionWorkOnNextListWait = new List<IntrinsicElectronicData>(); //#

	public List<IntrinsicElectronicData> _DirectionWorkOnNextList = new List<IntrinsicElectronicData>(); //#
	public List<IntrinsicElectronicData> _DirectionWorkOnNextListWait = new List<IntrinsicElectronicData>(); //#
	public bool UesAlternativeDirectionWorkOnNextList;

	public List<QEntry> ResistanceWorkOnNextList =
		new List<QEntry>(); //#

	public List<QEntry> ResistanceWorkOnNextListWait =
		new List<QEntry>(); //#

	public List<QEntry> _ResistanceWorkOnNextList =
		new List<QEntry>(); //#

	public List<QEntry> _ResistanceWorkOnNextListWait =
		new List<QEntry>(); //#

	public bool UesAlternativeResistanceWorkOnNextList;

	public int currentTick;
	public float tickRateComplete = 1f; //currently set to update every second
	public float tickRate;
	private const int Steps = 5;

	public List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>()
	{
		//Since you want the batteries to come after the radiation collectors so batteries don't put all there charge out then realise radiation collectors already doing it
		PowerTypeCategory.SolarPanel,
		PowerTypeCategory.RadiationCollector,
		PowerTypeCategory.PowerGenerator, //make sure unconditional supplies come first

		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.SolarPanelController,
		PowerTypeCategory.DepartmentBattery,
	};

	public List<PowerTypeCategory> UnconditionalSupplies = new List<PowerTypeCategory>()
	{
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.PowerGenerator,
		PowerTypeCategory.SolarPanel,
	};

	public HashSet<PowerTypeCategory> ReactiveSuppliesSet = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.SolarPanelController,
	};

	public HashSet<ElectricalNodeControl> TotalSupplies = new HashSet<ElectricalNodeControl>();

	public Dictionary<PowerTypeCategory, HashSet<ElectricalNodeControl>> AliveSupplies =
		new Dictionary<PowerTypeCategory, HashSet<ElectricalNodeControl>>()
			{ }; //Things that are supplying voltage

	public List<QueueAddSupply> SupplyToadd = new List<QueueAddSupply>();

	public HashSet<ElectricalNodeControl>
		PoweredDevices =
			new HashSet<ElectricalNodeControl>(); // things that may need electrical updates to react to voltage changes

	public Queue<ElectricalSynchronisationStorage> ToRemove = new Queue<ElectricalSynchronisationStorage>();

	public struct QueueAddSupply
	{
		public ElectricalNodeControl supply;
		public PowerTypeCategory category;
	};

#if UNITY_EDITOR
	public const string updateName = nameof(ElectricalSynchronisation) + "." + nameof(DoUpdate);

	public readonly string[] markerNames = new[]
	{
		nameof(IfStructureChange),
		nameof(PowerUpdateStructureChangeReact),
		nameof(PowerUpdateResistanceChange),
		nameof(PowerUpdateCurrentChange),
		nameof(PowerNetworkUpdate),
	}.Select(mn => $"{nameof(ElectricalSynchronisation)}.{mn}").ToArray();

	//private readonly ProfilerMarker[] markers = markerNames.Select(mn => new ProfilerMarker(mn)).ToArray();
#endif

	public void Reset()
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

	public void AddSupply(ElectricalNodeControl Supply, PowerTypeCategory category)
	{
		//SupplyToadd
		var Adding = new QueueAddSupply()
		{
			category = category,
			supply = Supply
		};
		SupplyToadd.Add(Adding);
	}

	private void InternalAddSupply(QueueAddSupply Adding)
	{
		if (!AliveSupplies.TryGetValue(Adding.category, out var aliveSup))
		{
			aliveSup = AliveSupplies[Adding.category] = new HashSet<ElectricalNodeControl>();
		}
		aliveSup.Add(Adding.supply);
		TotalSupplies.Add(Adding.supply);
	}

	public void RemoveSupply(ElectricalNodeControl Supply, PowerTypeCategory category)
	{
		ElectricalSynchronisationStorage QuickAdd = new ElectricalSynchronisationStorage();
		QuickAdd.device = Supply;
		QuickAdd.category = category;
		ToRemove.Enqueue(QuickAdd);
	}

	public void DoUpdate(bool Thread = true)
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

	private void DoTick(bool Thread = true)
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
				else
				{
					PowerNetworkUpdate();
				}

				break;
		}

		RemoveSupplies();
	}

	/// <summary>
	/// Remove all devices from <see cref="AliveSupplies"/> that were enqueued in <see cref="ToRemove"/>
	/// </summary>
	private void RemoveSupplies()
	{
		while (ToRemove.Count > 0)
		{
			var toRemove = ToRemove.Dequeue();
			if (AliveSupplies.TryGetValue(toRemove.category, out HashSet<ElectricalNodeControl> aliveSup) &&
				aliveSup.Remove(toRemove.device))
			{
				TotalSupplies.Remove(toRemove.device);
			}
		}
	}

	private void IfStructureChange()
	{
		if (!StructureChange)
		{
			return;
		}
		//Logger.Log("IfStructureChange");
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
	}

	/// <summary>
	/// This will generate directions
	/// </summary>
	private void PowerUpdateStructureChangeReact()
	{
		//Logger.Log("PowerUpdateStructureChangeReact");
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[OrderList[i]])
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
	private void PowerUpdateResistanceChange()
	{
		for (int i = InitialiseResistanceChange.Count - 1; i >= 0; i--)
		{
			if (i < InitialiseResistanceChange.Count)
			{
				InitialiseResistanceChange.ElementAt(i).InitialPowerUpdateResistance();
			}
		}

		InitialiseResistanceChange.Clear();

		for (int i = ResistanceChange.Count - 1; i >= 0; i--)
		{
			if (i < InitialiseResistanceChange.Count)
			{
				ResistanceChange.ElementAt(i).PowerUpdateResistanceChange();
			}
		}

		ResistanceChange.Clear();
		for (int i = 0; i < OrderList.Count; i++)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUResistanceChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)))
				{
					TheSupply.PowerUpdateResistanceChange();
					NUResistanceChange.Remove(TheSupply);
				}
			}
		}

		CircuitResistanceLoop();
	}

	/// <summary>
	/// Clear currents and Calculate the currents And voltage
	/// </summary>
	private void PowerUpdateCurrentChange()
	{
		//Logger.Log("PowerUpdateCurrentChange");
		for (int i = 0; i < UnconditionalSupplies.Count; i++)
		{
			foreach (ElectricalNodeControl TheSupply in AliveSupplies[OrderList[i]])
			{
				if (NUCurrentChange.Contains(TheSupply) && !(NUStructureChangeReact.Contains(TheSupply)) &&
				    !(NUResistanceChange.Contains(TheSupply)))
				{
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
	}

	private void ThreadedPowerNetworkUpdate()
	{
		//Logger.Log("ThreadedPowerNetworkUpdate");
		lock (ElectricalManager.ElectricalLock)
		{
			MainThreadProcess = true;
			Monitor.Wait(ElectricalManager.ElectricalLock);
		}
	}

	/// <summary>
	/// Sends updates to things that might need it
	/// </summary>
	public void PowerNetworkUpdate()
	{
		//Logger.Log("PowerNetworkUpdate");
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
	}

	public int NumberOfReactiveSupplies_f()
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

	public int NumberOfReactiveSupplies(IntrinsicElectronicData Devices)
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


	public void CircuitSearchLoop(ElectricalOIinheritance Thiswire)
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
				Break = false;
			}
		}
	}

	public void DirectionWorkOnNextListADD(IntrinsicElectronicData Thiswire)
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

	public void DirectionWorkOnNextListWaitADD(IntrinsicElectronicData Thiswire)
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

	public void DOCircuitSearchLoop(ElectricalOIinheritance GameObject,
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

	public void ResistanceWorkOnNextListADD(
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

	public void ResistanceWorkOnNextListWaitADD(
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

	public void CircuitResistanceLoop()
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

	public void DOCircuitResistanceLoop(
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

	public class QEntry
	{
		public ElectricalOIinheritance OIinheritance;
		public IntrinsicElectronicData InData;
	}
}