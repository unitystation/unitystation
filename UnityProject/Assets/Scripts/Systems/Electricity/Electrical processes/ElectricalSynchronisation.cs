using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Systems.Electricity.NodeModules;
using Objects.Electrical;

namespace Systems.Electricity
{
	public class ElectricalSynchronisationStorage
	{
		public PowerTypeCategory category;
		public ElectricalNodeControl device;
	}

	public class ElectricalSynchronisation : MonoBehaviour
	{
		public bool MainThreadStep = false;

		public CustomSampler sampler;

		private Thread thread;
		public ElectricalThread electricalThread;

		//What keeps electrical Ticking
		//so this is correlated to what has changed on the network, Needs to be optimised so (when one resistant source changes only that one updates its values currently the entire network updates their values)
		public bool StructureChange = true; //deals with the connections this will clear them out only

		//used for tracking deconstruction
		public HashSet<IntrinsicElectronicData> NUElectricalObjectsToDestroy = new HashSet<IntrinsicElectronicData>();

		//Used for poking the supplies to make up and down paths all the resistant sources
		public HashSet<ElectricalNodeControl> NUStructureChangeReact = new HashSet<ElectricalNodeControl>();

		//Used for all the resistant sources to broadcast there resistance  Used for supplies but could probably be combined with ResistanceChange
		public HashSet<ElectricalNodeControl> NUResistanceChange = new HashSet<ElectricalNodeControl>();

		public HashSet<ElectricalNodeControl> ResistanceChange = new HashSet<ElectricalNodeControl>();

		//Used for getting stuff to generate constant resistance values not really used properly
		public HashSet<ElectricalNodeControl> InitialiseResistanceChange = new HashSet<ElectricalNodeControl>();

		public HashSet<ElectricalNodeControl> NUCurrentChange = new HashSet<ElectricalNodeControl>();
		public HashSet<CableInheritance> CableUpdates = new HashSet<CableInheritance>();
		public CableInheritance CableToDestroy;

		public List<IntrinsicElectronicData> DirectionWorkOnNextList = new List<IntrinsicElectronicData>();
		public List<IntrinsicElectronicData> DirectionWorkOnNextListWait = new List<IntrinsicElectronicData>();

		public List<IntrinsicElectronicData> _DirectionWorkOnNextList = new List<IntrinsicElectronicData>();
		public List<IntrinsicElectronicData> _DirectionWorkOnNextListWait = new List<IntrinsicElectronicData>();
		public bool UesAlternativeDirectionWorkOnNextList;

		public int currentTick;

		private readonly List<PowerTypeCategory> OrderList = new List<PowerTypeCategory>()
	{
		//Since you want the batteries to come after the radiation collectors so batteries don't put all there charge out then realise radiation collectors already doing it
		PowerTypeCategory.Turbine,
		PowerTypeCategory.SolarPanel,
		PowerTypeCategory.RadiationCollector,
		PowerTypeCategory.PowerGenerator, //make sure unconditional supplies come first

		PowerTypeCategory.SMES, //Then conditional supplies With the hierarchy you want
		PowerTypeCategory.SolarPanelController,
		PowerTypeCategory.DepartmentBattery,
	};

		private readonly List<PowerTypeCategory> UnconditionalSupplies = new List<PowerTypeCategory>()
	{
		PowerTypeCategory.RadiationCollector, //make sure unconditional supplies come first
		PowerTypeCategory.Turbine,
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

		//Things that are supplying voltage
		public Dictionary<PowerTypeCategory, HashSet<ElectricalNodeControl>> AliveSupplies = new Dictionary<PowerTypeCategory, HashSet<ElectricalNodeControl>>();

		public List<QueueAddSupply> SupplyToadd = new List<QueueAddSupply>();

		// things that may need electrical updates to react to voltage changes
		public HashSet<ElectricalNodeControl> PoweredDevices = new HashSet<ElectricalNodeControl>();

		public Queue<ElectricalSynchronisationStorage> ToRemove = new Queue<ElectricalSynchronisationStorage>();

		public struct QueueAddSupply
		{
			public ElectricalNodeControl supply;
			public PowerTypeCategory category;
		};

		ElectricalSynchronisation()
		{
			sampler = CustomSampler.Create("ElectricalStep");
		}
		public void StartSim()
		{
			foreach (var category in OrderList)
			{
				AliveSupplies[category] = new HashSet<ElectricalNodeControl>();
			}
			electricalThread = gameObject.AddComponent<ElectricalThread>();
			electricalThread.StartThread();
		}

		public void StopSim()
		{
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

			SupplyToadd.Clear();
			AliveSupplies.Clear();
			TotalSupplies.Clear();
			ToRemove.Clear();
		}

		public void AddSupply(ElectricalNodeControl supply, PowerTypeCategory category)
		{
			var adding = new QueueAddSupply()
			{
				category = category,
				supply = supply
			};
			SupplyToadd.Add(adding);
		}

		private void InternalAddSupply(QueueAddSupply adding)
		{
			if (!AliveSupplies.TryGetValue(adding.category, out var aliveSup))
			{
				aliveSup = AliveSupplies[adding.category] = new HashSet<ElectricalNodeControl>();
			}
			aliveSup.Add(adding.supply);
			TotalSupplies.Add(adding.supply);
		}

		public void RemoveSupply(ElectricalNodeControl supply, PowerTypeCategory category)
		{
			var quickAdd = new ElectricalSynchronisationStorage();
			quickAdd.device = supply;
			quickAdd.category = category;
			ToRemove.Enqueue(quickAdd);
		}

		public void DoTick()
		{
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
					MainThreadStep = true;
					break;
				case 4:
					//main thread only step
					PowerNetworkUpdate();
					MainThreadStep = false;
					break;
			}
			RemoveSupplies();
			currentTick++;
			if (currentTick > 4)
			{
				currentTick = 0;
			}
		}

		/// <summary>
		/// Remove all devices from <see cref="AliveSupplies"/> that were enqueued in <see cref="ToRemove"/>
		/// </summary>
		private void RemoveSupplies()
		{
			while (ToRemove.Count > 0)
			{
				var toRemove = ToRemove.Dequeue();
				if (AliveSupplies.TryGetValue(toRemove.category, out var aliveSup) &&
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
			StructureChange = false;
			foreach (var categoryHashset in AliveSupplies)
			{
				foreach (var supply in categoryHashset.Value)
				{
					supply.PowerUpdateStructureChange();
				}
			}

			foreach (var device in PoweredDevices)
			{
				device.PowerUpdateStructureChange();
			}
		}

		/// <summary>
		/// This will generate directions
		/// </summary>
		private void PowerUpdateStructureChangeReact()
		{
			foreach (var supply in SupplyToadd)
			{
				InternalAddSupply(supply);
			}
			SupplyToadd.Clear();

			foreach (var categoryHashset in AliveSupplies)
			{
				foreach (var supply in categoryHashset.Value)
				{
					if (NUStructureChangeReact.Contains(supply))
					{
						supply.PowerUpdateStructureChangeReact();
						NUStructureChangeReact.Remove(supply);
					}
				}
			}
		}

		/// <summary>
		/// Clear  resistance and Calculate the resistance for everything
		/// </summary>
		private void PowerUpdateResistanceChange()
		{
			for (var i = InitialiseResistanceChange.Count - 1; i >= 0; i--)
			{
				InitialiseResistanceChange.ElementAt(i).InitialPowerUpdateResistance();
			}

			InitialiseResistanceChange.Clear();

			for (var i = ResistanceChange.Count - 1; i >= 0; i--)
			{
				ResistanceChange.ElementAt(i).PowerUpdateResistanceChange();
			}
			ResistanceChange.Clear();

			foreach (var categoryHashset in AliveSupplies)
			{
				foreach (var supply in categoryHashset.Value)
				{
					if (NUResistanceChange.Contains(supply) && !(NUStructureChangeReact.Contains(supply)))
					{
						supply.PowerUpdateResistanceChange();
						NUResistanceChange.Remove(supply);
					}
				}
			}
		}

		/// <summary>
		/// Clear currents and Calculate the currents And voltage
		/// </summary>
		private void PowerUpdateCurrentChange()
		{
			for (var i = 0; i < UnconditionalSupplies.Count; i++)
			{
				var categoryHashset = AliveSupplies[OrderList[i]];
				foreach (var supply in categoryHashset)
				{
					if (NUCurrentChange.Contains(supply) && !NUStructureChangeReact.Contains(supply) && !NUResistanceChange.Contains(supply))
					{
						//Does all the updates for the constant sources since they don't have to worry about other supplies being on or off since they just go steaming ahead
						supply.PowerUpdateCurrentChange();
						NUCurrentChange.Remove(supply);
					}
				}
			}

			var doneSupplies = new HashSet<ElectricalNodeControl>();
			ElectricalNodeControl lowestReactive = null;
			var lowestReactiveInt = 9999;
			var nodeToRemove = new List<ElectricalNodeControl>();

			//This is to calculate the lowest number of supplies that are above the reactive supply so therefore the one that needs to be updated first
			while (NumberOfReactiveSupplies_f() > 0)
			{
				foreach (var supply in NUCurrentChange)
				{
					if (!doneSupplies.Contains(supply))
					{
						if (TotalSupplies.Contains(supply))
						{
							if (ReactiveSuppliesSet.Contains(supply.Node.InData.Categorytype))
							{
								if (NUCurrentChange.Contains(supply) && !(NUStructureChangeReact.Contains(supply)) &&
									!(NUResistanceChange.Contains(supply)))
								{
									if (lowestReactive == null)
									{
										lowestReactive = supply;
										lowestReactiveInt = NumberOfReactiveSupplies(supply.Node.InData);
									}
									else if (lowestReactiveInt > NumberOfReactiveSupplies(supply.Node.InData))
									{
										lowestReactive = supply;
										lowestReactiveInt = NumberOfReactiveSupplies(supply.Node.InData);
									}
								}
								else
								{
									nodeToRemove.Add(supply);
								}
							}
						}
						else
						{
							nodeToRemove.Add(supply);
						}
					}
					else
					{
						nodeToRemove.Add(supply);
					}
				}

				if (lowestReactive != null)
				{
					lowestReactive.PowerUpdateCurrentChange();
					NUCurrentChange.Remove(lowestReactive);
					doneSupplies.Add(lowestReactive);
				}

				lowestReactive = null;
				lowestReactiveInt = 9999;
				foreach (var node in nodeToRemove)
				{
					NUCurrentChange.Remove(node);
				}

				nodeToRemove = new List<ElectricalNodeControl>();
			}
		}

		/// <summary>
		/// Sends updates to things that might need it
		/// </summary>
		public void PowerNetworkUpdate()
		{
			foreach (var categoryHashset in AliveSupplies)
			{
				foreach (var supply in categoryHashset.Value)
				{
					supply.PowerNetworkUpdate();
				}
			}

			foreach (var device in PoweredDevices)
			{
				device.PowerNetworkUpdate();
			}

			foreach (var device in CableUpdates)
			{
				device.PowerNetworkUpdate();
			}

			CableUpdates.Clear();


			if (CableToDestroy != null)
			{
				CableToDestroy.wireConnect.DestroyThisPlease();
				CableToDestroy = null;
			}

			// Structure change and stuff
			foreach (var device in NUElectricalObjectsToDestroy)
			{
				device.DestroyingThisNow();
			}

			NUElectricalObjectsToDestroy.Clear();
		}

		private int NumberOfReactiveSupplies_f()
		{
			var counting = 0;
			foreach (var device in NUCurrentChange)
			{
				if (device == null)
				{
					continue;
				}
				if (ReactiveSuppliesSet.Contains(device.Node.InData.Categorytype))
				{
					counting++;
				}
			}
			return counting;
		}

		private int NumberOfReactiveSupplies(IntrinsicElectronicData devices)
		{
			var counting = 0;
			foreach (var device in devices.Data.ResistanceToConnectedDevices)
			{
				if (ReactiveSuppliesSet.Contains(device.Key.Data.InData.Categorytype))
				{
					counting++;
				}
			}

			return counting;
		}

		public void DirectionWorkOnNextListADD(IntrinsicElectronicData wire)
		{
			if (UesAlternativeDirectionWorkOnNextList)
			{
				_DirectionWorkOnNextList.Add(wire);
			}
			else
			{
				DirectionWorkOnNextList.Add(wire);
			}
		}

		public void DirectionWorkOnNextListWaitADD(IntrinsicElectronicData wire)
		{
			if (UesAlternativeDirectionWorkOnNextList)
			{
				_DirectionWorkOnNextListWait.Add(wire);
			}
			else
			{
				DirectionWorkOnNextListWait.Add(wire);
			}
		}

		public void CircuitSearchLoop(ElectricalOIinheritance wire)
		{
			InputOutputFunctions.DirectionOutput(wire, wire.InData);
			while (true)
			{
				UesAlternativeDirectionWorkOnNextList = false;
				CircuitSearch(wire, _DirectionWorkOnNextList, _DirectionWorkOnNextListWait);

				UesAlternativeDirectionWorkOnNextList = true;
				CircuitSearch(wire, DirectionWorkOnNextList, DirectionWorkOnNextListWait);

				if (DirectionWorkOnNextList.Count <= 0
				    && DirectionWorkOnNextListWait.Count <= 0
				    && _DirectionWorkOnNextList.Count <= 0
				    && _DirectionWorkOnNextListWait.Count <= 0)
				{
					break;
				}
			}
		}

		private void CircuitSearch(ElectricalOIinheritance wire,
			List<IntrinsicElectronicData> iterateDirectionWorkOnNextList,
			List<IntrinsicElectronicData> directionWorkOnNextListWait)
		{
			foreach (var circuit in iterateDirectionWorkOnNextList)
			{
				circuit.DirectionOutput(wire);
			}

			iterateDirectionWorkOnNextList.Clear();
			if (DirectionWorkOnNextList.Count <= 0 && _DirectionWorkOnNextList.Count <= 0)
			{
				foreach (var circuit in directionWorkOnNextListWait)
				{
					circuit.DirectionOutput(wire);
				}
				directionWorkOnNextListWait.Clear();
			}
		}
	}
}
