using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core;
using US13.Core.ObjectConnection;
using US13.Managers.UpdateManager;
using US13.Systems.Electricity.NodeModules;

namespace US13.Systems.Electricity.PowerSupplies
{
	public class SolarPanelController : NetworkBehaviour, IMultitoolMasterable
	{
		public ModuleSupplyingDevice ModuleSupply;
		public float UpdateRate = 10f;
		public float MaxCurrent = 0.06f;
		[SerializeField] private List<SolarPanel> connectedPanels;

		public MultitoolConnectionType ConType { get; } = MultitoolConnectionType.SolarPanel;
		public bool CanRelink { get; } = true;
		public int MaxDistance { get; } = 64;
		public bool IgnoreMaxDistanceMapper { get; } = false;

		private void Awake()
		{
			ComponentsTracker<SolarPanelController>.RegisterInstance(this);
		}

		private void Start()
		{
			ModuleSupply ??= GetComponent<ModuleSupplyingDevice>();
			if (isServer == false) return;
			UpdateManager.Add(UpdateMe, UpdateRate);
		}

		private void OnDestroy()
		{
			ComponentsTracker<SolarPanelController>.UnregisterInstance(this);
			if (isServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			var watts = 0;
			foreach (var panel in connectedPanels)
			{
				watts += panel.LastProducedWatts;
			}
			ModuleSupply.ProducingWatts = watts;
			ModuleSupply.Maxcurrent = MaxCurrent;
		}

		public bool AddDevice(SolarPanel panel)
		{
			if (connectedPanels.Contains(panel)) return false;
			connectedPanels.Add(panel);
			panel.Controller = this;
			return true;
		}

		public void RemoveDevice(SolarPanel panel)
		{
			if (connectedPanels.Contains(panel) == false) return;
			panel.Controller = null;
			connectedPanels.Remove(panel);
		}
	}
}